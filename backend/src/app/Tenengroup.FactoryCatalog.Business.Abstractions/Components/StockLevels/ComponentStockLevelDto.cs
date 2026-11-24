using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;

public record ComponentStockLevelDto(
    string ErpId,
    string Name,
    string? Family,
    string FactoryId,
    decimal WarehouseStock,
    decimal ProductionFloorStock,
    decimal TotalStock,
    decimal? CriticalLevel,
    bool IsCriticalLevelOverride,
    StockAlertStatus Status,
    string? ManualOosSetBy,
    DateTime? ManualOosSetAt,
    string? ManualOosNote);
