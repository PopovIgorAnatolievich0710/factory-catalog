using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Headers = Tenengroup.FactoryCatalog.Business.Processes.Import.ProcessImportFileReader;

namespace Tenengroup.FactoryCatalog.Business.Processes.Import;

internal record ProcessImportItem(string ProcessId, ProcessSpecification Specification);

internal record ProcessFactoryImportItem(string ProcessId, string FactoryId, ProcessFactorySetup Setup);

internal record ProcessImportPlan(
    int TotalRows,
    List<ProcessImportRowErrorDto> Errors,
    List<ProcessImportItem> Processes,
    List<ProcessFactoryImportItem> Factories,
    List<Process> ExistingProcesses)
{
    public ProcessImportValidationDto ToValidation()
    {
        var invalidRows = Errors.Select(e => (IsFactoryRow(e), e.Row)).Distinct().Count();

        return new ProcessImportValidationDto(TotalRows, Math.Max(0, TotalRows - invalidRows), Errors);
    }

    private static bool IsFactoryRow(ProcessImportRowErrorDto error)
    {
        return error.Field?.StartsWith(Headers.FactoriesSheet + ":", StringComparison.Ordinal) ?? false;
    }
}

public class ProcessImportPlanner(IFactoryCatalogDbContext db)
{
    private static readonly Dictionary<string, ProcessUnit> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["minutes"] = ProcessUnit.Minutes,
        ["seconds"] = ProcessUnit.Seconds,
        ["hours"] = ProcessUnit.Hours,
        ["units"] = ProcessUnit.Units,
        ["pieces"] = ProcessUnit.Pieces,
        ["cm"] = ProcessUnit.Cm,
        ["mm"] = ProcessUnit.Mm,
        ["cm2"] = ProcessUnit.Cm2,
        ["cm²"] = ProcessUnit.Cm2,
        ["mm2"] = ProcessUnit.Mm2,
        ["mm²"] = ProcessUnit.Mm2
    };

    private static readonly Dictionary<string, string> ProcessFieldHeaders = new()
    {
        [ProcessRules.ProcessIdField] = Headers.ProcessIdHeader,
        [ProcessRules.NameField] = Headers.NameHeader,
        [ProcessRules.TypeField] = Headers.TypeHeader,
        [ProcessRules.StatusField] = Headers.StatusHeader,
        [ProcessRules.DescriptionField] = Headers.DescriptionHeader
    };

    private static readonly Dictionary<string, string> FactoryFieldHeaders = new()
    {
        [ProcessRules.MachineTypeField] = Headers.MachineTypeHeader,
        [ProcessRules.LaborCostAmountField] = Headers.LaborCostHeader,
        [ProcessRules.LaborCostCurrencyField] = Headers.CurrencyHeader,
        [ProcessRules.MaxPerDayField] = Headers.MaxPerDayHeader
    };

    private static readonly string[] TrueValues = ["true", "yes", "y", "1"];
    private static readonly string[] FalseValues = ["false", "no", "n", "0"];

    internal async Task<ProcessImportPlan> Build(byte[] content, CancellationToken cancellationToken)
    {
        var file = ProcessImportFileReader.Read(content);
        var existing = await db.Processes.ToListAsync(cancellationToken);
        var plan = new ProcessImportPlan(file.Processes.Count + file.Factories.Count, file.Errors, [], [], existing);

        if (file.Errors.Count > 0)
        {
            return plan;
        }

        if (plan.TotalRows == 0)
        {
            plan.Errors.Add(new ProcessImportRowErrorDto(0, null, "The file has no data rows."));
            return plan;
        }

        var existingById = existing.ToDictionary(p => p.ProcessId);

        foreach (var row in file.Processes)
        {
            AddProcess(plan, row, existingById);
        }

        AddNameConflicts(plan, file.Processes, existing);

        var factoryIds = await db.Factories.AsNoTracking().Select(f => f.Id).ToListAsync(cancellationToken);
        var plannedTypes = plan.Processes.ToDictionary(p => p.ProcessId, p => p.Specification.Type);
        var seenPairs = new HashSet<ProcessFactoryKey>();

        foreach (var row in file.Factories)
        {
            AddFactory(plan, row, existingById, plannedTypes, factoryIds, seenPairs);
        }

        return plan;
    }

    private static void AddProcess(ProcessImportPlan plan, ProcessImportRawRow row, Dictionary<string, Process> existingById)
    {
        var errors = new List<ProcessImportRowErrorDto>();
        var processId = ProcessRules.NormalizeProcessId(row.ProcessId);
        existingById.TryGetValue(processId, out var existing);

        errors.AddRange(ProcessRules.ProcessIdErrors(processId).Select(e => ToError(row.Row, e, ProcessFieldHeaders, h => h)));

        if (plan.Processes.Any(p => p.ProcessId == processId))
        {
            errors.Add(new ProcessImportRowErrorDto(row.Row, Headers.ProcessIdHeader, $"Process ID '{processId}' is repeated in the file."));
        }

        if (!Enum.TryParse<ProcessType>(row.Type, true, out var type) || !Enum.IsDefined(type))
        {
            errors.Add(new ProcessImportRowErrorDto(row.Row, Headers.TypeHeader, "Type must be Machine or Work."));
        }

        if (!Units.TryGetValue(row.MeasuredIn, out var measuredIn))
        {
            errors.Add(new ProcessImportRowErrorDto(
                row.Row, Headers.MeasuredInHeader, "Measured in must be minutes, seconds, hours, units, pieces, cm, mm, cm2 or mm2."));
        }

        var status = existing?.Status ?? ProcessStatus.Active;
        if (row.Status.Length > 0 && (!Enum.TryParse(row.Status, true, out status) || !Enum.IsDefined(status)))
        {
            errors.Add(new ProcessImportRowErrorDto(row.Row, Headers.StatusHeader, "Status must be Active or Inactive."));
        }

        var isParsed = errors.Count == 0;
        var specification = ProcessSpecification.Create(row.Name, type, measuredIn, status, row.Description);

        errors.AddRange(ProcessRules.SpecificationErrors(processId, specification)
            .Where(e => isParsed || e.Field is ProcessRules.NameField or ProcessRules.DescriptionField)
            .Select(e => ToError(row.Row, e, ProcessFieldHeaders, h => h)));

        plan.Errors.AddRange(errors);

        if (errors.Count == 0)
        {
            plan.Processes.Add(new ProcessImportItem(processId, specification));
        }
    }

    private static void AddNameConflicts(ProcessImportPlan plan, List<ProcessImportRawRow> rows, List<Process> existing)
    {
        var fileIds = rows.Select(r => ProcessRules.NormalizeProcessId(r.ProcessId)).ToHashSet();
        var takenNames = existing
            .Where(p => !fileIds.Contains(p.ProcessId))
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            var name = row.Name.Trim();

            if (!takenNames.Add(name))
            {
                plan.Errors.Add(new ProcessImportRowErrorDto(row.Row, Headers.NameHeader, $"A process named '{name}' already exists."));
                plan.Processes.RemoveAll(p => p.ProcessId == ProcessRules.NormalizeProcessId(row.ProcessId));
            }
        }
    }

    private static void AddFactory(
        ProcessImportPlan plan,
        ProcessFactoryImportRawRow row,
        Dictionary<string, Process> existingById,
        Dictionary<string, ProcessType> plannedTypes,
        List<string> factoryIds,
        HashSet<ProcessFactoryKey> seenPairs)
    {
        var errors = new List<ProcessImportRowErrorDto>();
        var processId = ProcessRules.NormalizeProcessId(row.ProcessId);
        var hasType = plannedTypes.TryGetValue(processId, out var processType);

        if (!hasType && existingById.TryGetValue(processId, out var existing))
        {
            hasType = true;
            processType = existing.Type;
        }

        if (!hasType)
        {
            Add(Headers.ProcessIdHeader, $"Process '{processId}' is neither in the catalog nor a valid row of the {Headers.ProcessesSheet} sheet.");
        }

        if (!factoryIds.Contains(row.FactoryId))
        {
            Add(Headers.FactoryIdHeader, $"Factory '{row.FactoryId}' does not exist.");
        }
        else if (!seenPairs.Add(new ProcessFactoryKey(processId, row.FactoryId)))
        {
            Add(Headers.FactoryIdHeader, $"Process '{processId}' and factory '{row.FactoryId}' are repeated in the file.");
        }

        var runsHere = TrueValues.Contains(row.RunsHere, StringComparer.OrdinalIgnoreCase);
        if (!runsHere && !FalseValues.Contains(row.RunsHere, StringComparer.OrdinalIgnoreCase))
        {
            Add(Headers.RunsHereHeader, "Runs here must be Yes or No.");
        }

        var laborCost = ParseNumber(row.LaborCost, Headers.LaborCostHeader);
        var maxPerDay = ParseNumber(row.MaxPerDay, Headers.MaxPerDayHeader);
        var setup = ProcessFactorySetup.Create(runsHere, row.MachineType, laborCost, row.Currency, maxPerDay);

        if (hasType)
        {
            errors.AddRange(ProcessRules.FactorySetupErrors(processType, setup)
                .Select(e => ToError(row.Row, e, FactoryFieldHeaders, Headers.FactoriesField)));
        }

        plan.Errors.AddRange(errors);

        if (errors.Count == 0)
        {
            plan.Factories.Add(new ProcessFactoryImportItem(processId, row.FactoryId, setup));
        }

        return;

        void Add(string header, string message)
        {
            errors.Add(new ProcessImportRowErrorDto(row.Row, Headers.FactoriesField(header), message));
        }

        decimal? ParseNumber(string text, string header)
        {
            if (text.Length == 0)
            {
                return null;
            }

            if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            Add(header, $"'{text}' is not a number.");
            return null;
        }
    }

    private static ProcessImportRowErrorDto ToError(
        int row,
        ProcessRuleError error,
        Dictionary<string, string> fieldHeaders,
        Func<string, string> toField)
    {
        return new ProcessImportRowErrorDto(row, toField(fieldHeaders.GetValueOrDefault(error.Field, error.Field)), error.Message);
    }
}
