using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record ComponentListItemDto(
    string ErpId,
    string Name,
    string? Family,
    string? Material,
    ComponentStatus ComponentStatus,
    StockAlertStatus Status,
    IReadOnlyCollection<string> StatusFactoryIds,
    bool ActionRequired,
    decimal TotalStock,
    decimal? CriticalLevel,
    bool HasCriticalLevelOverrides,
    decimal? UnitCost,
    string? UnitCostCurrency,
    int SkusUsing,
    IReadOnlyDictionary<string, decimal> StockByFactory,
    DateTime? LastSapSyncAt);
