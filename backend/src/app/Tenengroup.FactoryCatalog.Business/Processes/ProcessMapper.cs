using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Processes;

internal static class ProcessMapper
{
    public static ProcessCapacityCellDto ToCapacityCell(string factoryId, ProcessFactory row, int used)
    {
        var runsHere = row?.RunsHere ?? false;
        var capacityPct = row?.CapacityPct ?? ProcessFactory.FullCapacityPct;

        return new ProcessCapacityCellDto(
            factoryId,
            runsHere,
            capacityPct,
            ProcessCapacityCalculator.Status(capacityPct),
            row?.CapacityReason,
            row?.ExpectedRecovery,
            row?.CapacityUpdatedBy,
            row?.CapacityUpdatedAt,
            row?.MaxPerDay,
            runsHere ? ProcessCapacityCalculator.Today(row.MaxPerDay, capacityPct, used) : null);
    }

    public static ProcessFactoryDto ToFactoryDto(Factory factory, ProcessFactory row, int used, bool canViewCost)
    {
        var cell = ToCapacityCell(factory.Id, row, used);

        return new ProcessFactoryDto(
            factory.Id,
            factory.Name,
            cell.RunsHere,
            row?.MachineType,
            cell.CapacityPct,
            cell.Status,
            cell.Reason == null && cell.ExpectedRecovery == null ? null : new CapacityNoteDto(cell.Reason, cell.ExpectedRecovery),
            cell.UpdatedBy,
            cell.UpdatedAt,
            canViewCost && row?.LaborCostAmount != null
                ? new LaborCostDto(row.LaborCostAmount.Value, row.LaborCostCurrency, row.LaborCostEffectiveFrom)
                : null,
            cell.MaxPerDay,
            cell.Today);
    }
}
