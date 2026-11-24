using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Processes;

internal static class ProcessChanges
{
    public const string CreatedField = "process";
    public const string CreatedValue = "Created";

    public static bool ApplySpecification(
        Process process,
        IEnumerable<ProcessFactory> factoryRows,
        ProcessSpecification specification,
        IHistoryWriter historyWriter,
        HistorySource source,
        DateTime now)
    {
        var id = process.ProcessId;
        var changed = process.Name != specification.Name
                      || process.Type != specification.Type
                      || process.MeasuredIn != specification.MeasuredIn
                      || process.Status != specification.Status
                      || process.Description != specification.Description;

        if (!changed)
        {
            return false;
        }

        historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.NameField, process.Name, specification.Name, source);
        historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.TypeField, process.Type.ToString(), specification.Type.ToString(), source);
        historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.MeasuredInField, process.MeasuredIn.ToString(), specification.MeasuredIn.ToString(), source);
        historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.StatusField, process.Status.ToString(), specification.Status.ToString(), source);
        historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.DescriptionField, process.Description, specification.Description, source);

        if (specification.Type != ProcessType.Machine)
        {
            foreach (var row in factoryRows.Where(r => r.ProcessId == id && r.MachineType != null))
            {
                historyWriter.Record(HistoryEntityTypes.Process, id, ProcessRules.MachineTypeField, row.MachineType, null, source, row.FactoryId);
                row.MachineType = null;
                row.UpdatedAt = now;
            }
        }

        process.Name = specification.Name;
        process.Type = specification.Type;
        process.MeasuredIn = specification.MeasuredIn;
        process.Status = specification.Status;
        process.Description = specification.Description;
        process.UpdatedAt = now;

        return true;
    }

    public static bool ApplyFactorySetup(
        ProcessFactory row,
        ProcessFactorySetup setup,
        IHistoryWriter historyWriter,
        HistorySource source,
        DateTime now)
    {
        var costChanged = row.LaborCostAmount != setup.LaborCostAmount || row.LaborCostCurrency != setup.LaborCostCurrency;
        var changed = costChanged
                      || row.RunsHere != setup.RunsHere
                      || row.MachineType != setup.MachineType
                      || row.MaxPerDay != setup.MaxPerDay;

        if (!changed)
        {
            return false;
        }

        Record(ProcessRules.RunsHereField, row.RunsHere, setup.RunsHere);
        Record(ProcessRules.MachineTypeField, row.MachineType, setup.MachineType);
        Record(ProcessRules.LaborCostAmountField, row.LaborCostAmount, setup.LaborCostAmount);
        Record(ProcessRules.LaborCostCurrencyField, row.LaborCostCurrency, setup.LaborCostCurrency);
        Record(ProcessRules.MaxPerDayField, row.MaxPerDay, setup.MaxPerDay);

        row.RunsHere = setup.RunsHere;
        row.MachineType = setup.MachineType;
        row.MaxPerDay = setup.MaxPerDay;

        if (costChanged)
        {
            row.LaborCostAmount = setup.LaborCostAmount;
            row.LaborCostCurrency = setup.LaborCostCurrency;
            row.LaborCostEffectiveFrom = setup.LaborCostAmount == null ? null : DateOnly.FromDateTime(now);
        }

        row.UpdatedAt = now;

        return true;

        void Record(string field, object oldValue, object newValue)
        {
            historyWriter.Record(HistoryEntityTypes.Process, row.ProcessId, field, oldValue, newValue, source, row.FactoryId);
        }
    }

    public static ProcessFactory NewFactoryRow(string processId, string factoryId, DateTime now)
    {
        return new ProcessFactory
        {
            ProcessId = processId,
            FactoryId = factoryId,
            CapacityPct = ProcessFactory.FullCapacityPct,
            UpdatedAt = now
        };
    }
}
