using Tenengroup.FactoryCatalog.Domains.Processes;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public record ProcessListItemDto(
    string ProcessId,
    string Name,
    ProcessType Type,
    ProcessUnit MeasuredIn,
    ProcessStatus Status,
    string? MachineType,
    IReadOnlyCollection<string> RunningFactoryIds,
    int? LowestCapacityPct,
    decimal? AvgLaborCost,
    string? AvgLaborCostCurrency,
    int SkusUsing);
