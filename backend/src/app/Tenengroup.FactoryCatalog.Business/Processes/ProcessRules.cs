using System.Text.RegularExpressions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Processes;

internal record ProcessRuleError(string Field, string Message);

internal record ProcessSpecification(
    string Name,
    ProcessType Type,
    ProcessUnit MeasuredIn,
    ProcessStatus Status,
    string Description)
{
    public static ProcessSpecification Create(
        string name,
        ProcessType type,
        ProcessUnit measuredIn,
        ProcessStatus status,
        string description)
    {
        return new ProcessSpecification(
            name?.Trim() ?? string.Empty,
            type,
            measuredIn,
            status,
            ProcessRules.NullIfBlank(description));
    }
}

internal record ProcessFactorySetup(
    bool RunsHere,
    string MachineType,
    decimal? LaborCostAmount,
    string LaborCostCurrency,
    decimal? MaxPerDay)
{
    public static ProcessFactorySetup Create(
        bool runsHere,
        string machineType,
        decimal? laborCostAmount,
        string laborCostCurrency,
        decimal? maxPerDay)
    {
        return new ProcessFactorySetup(
            runsHere,
            ProcessRules.NullIfBlank(machineType),
            laborCostAmount,
            laborCostAmount == null ? null : ProcessRules.NullIfBlank(laborCostCurrency)?.ToUpperInvariant(),
            maxPerDay);
    }
}

internal static partial class ProcessRules
{
    public const string ProcessIdField = "processId";
    public const string NameField = "name";
    public const string TypeField = "type";
    public const string MeasuredInField = "measuredIn";
    public const string StatusField = "status";
    public const string DescriptionField = "description";
    public const string FactoryIdField = "factoryId";
    public const string RunsHereField = "runsHere";
    public const string MachineTypeField = "machineType";
    public const string LaborCostAmountField = "laborCostAmount";
    public const string LaborCostCurrencyField = "laborCostCurrency";
    public const string MaxPerDayField = "maxPerDay";
    public const string CapacityPctField = "capacityPct";
    public const string CapacityReasonField = "capacityReason";
    public const string ExpectedRecoveryField = "expectedRecovery";

    public const int CapacityReasonMaxLength = 1000;

    private const int ProcessIdMaxLength = 50;
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 1000;
    private const int MachineTypeMaxLength = 100;
    private const int CurrencyLength = 3;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_.-]*$")]
    private static partial Regex ProcessIdPattern();

    [GeneratedRegex("[^A-Z0-9]+")]
    private static partial Regex NonIdCharacters();

    public static string NullIfBlank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string NormalizeProcessId(string processId)
    {
        return processId?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static string DeriveProcessId(string name)
    {
        var id = NonIdCharacters().Replace(NormalizeProcessId(name), "_").Trim('_');

        return id.Length > ProcessIdMaxLength ? id[..ProcessIdMaxLength].TrimEnd('_') : id;
    }

    public static List<ProcessRuleError> ProcessIdErrors(string processId)
    {
        var errors = new List<ProcessRuleError>();

        if (string.IsNullOrEmpty(processId))
        {
            errors.Add(new ProcessRuleError(ProcessIdField, "Process ID is required."));
        }
        else if (processId.Length > ProcessIdMaxLength)
        {
            errors.Add(new ProcessRuleError(ProcessIdField, $"Process ID must be at most {ProcessIdMaxLength} characters."));
        }
        else if (!ProcessIdPattern().IsMatch(processId))
        {
            errors.Add(new ProcessRuleError(ProcessIdField, "Process ID may contain only letters, digits, underscore, dash and dot."));
        }

        return errors;
    }

    public static List<ProcessRuleError> SpecificationErrors(string processId, ProcessSpecification specification)
    {
        var errors = new List<ProcessRuleError>();

        if (specification.Name.Length == 0)
        {
            errors.Add(new ProcessRuleError(NameField, "Name is required."));
        }
        else if (specification.Name.Length > NameMaxLength)
        {
            errors.Add(new ProcessRuleError(NameField, $"Name must be at most {NameMaxLength} characters."));
        }

        if (specification.Description?.Length > DescriptionMaxLength)
        {
            errors.Add(new ProcessRuleError(DescriptionField, $"Description must be at most {DescriptionMaxLength} characters."));
        }

        if (processId == Process.PackingProcessId)
        {
            if (specification.Type != ProcessType.Work)
            {
                errors.Add(new ProcessRuleError(TypeField, "Packing must stay a work process."));
            }

            if (specification.Status != ProcessStatus.Active)
            {
                errors.Add(new ProcessRuleError(StatusField, "Packing cannot be deactivated: its cost is part of every production quote."));
            }
        }

        return errors;
    }

    public static List<ProcessRuleError> FactorySetupErrors(ProcessType processType, ProcessFactorySetup setup)
    {
        var errors = new List<ProcessRuleError>();

        if (setup.MachineType != null && processType != ProcessType.Machine)
        {
            errors.Add(new ProcessRuleError(MachineTypeField, "Machine type applies to machine processes only."));
        }
        else if (setup.MachineType?.Length > MachineTypeMaxLength)
        {
            errors.Add(new ProcessRuleError(MachineTypeField, $"Machine type must be at most {MachineTypeMaxLength} characters."));
        }

        if (setup.LaborCostAmount < 0)
        {
            errors.Add(new ProcessRuleError(LaborCostAmountField, "Labor cost cannot be negative."));
        }

        if (setup.LaborCostAmount != null
            && (setup.LaborCostCurrency?.Length != CurrencyLength || !setup.LaborCostCurrency.All(char.IsAsciiLetterUpper)))
        {
            errors.Add(new ProcessRuleError(LaborCostCurrencyField, "A 3-letter currency code is required with a labor cost."));
        }

        if (setup.MaxPerDay < 0)
        {
            errors.Add(new ProcessRuleError(MaxPerDayField, "Maximum daily output cannot be negative."));
        }

        return errors;
    }

    public static void ThrowIfAny(IReadOnlyCollection<ProcessRuleError> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors
                .GroupBy(e => e.Field)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToList()));
        }
    }
}
