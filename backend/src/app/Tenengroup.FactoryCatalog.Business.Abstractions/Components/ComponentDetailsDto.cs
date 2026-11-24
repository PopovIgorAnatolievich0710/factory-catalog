using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components;

public record ComponentDetailsDto(
    string ErpId,
    string Name,
    ComponentStatus Status,
    string? Family,
    string? Material,
    ComponentMeasureDto Measure,
    ComponentUnitOfMeasureDto UnitOfMeasure,
    string? CountryOfOrigin,
    string? HsCode,
    ComponentUnitCostDto? UnitCost,
    decimal? CriticalStockLevel,
    StockAlertStatus StockStatus,
    IReadOnlyCollection<string> StockStatusFactoryIds,
    bool ActionRequired,
    IReadOnlyCollection<ComponentFactoryDto> Factories,
    IReadOnlyCollection<ComponentAlternativeDto> Alternatives,
    IReadOnlyCollection<ComponentSkuUsageDto> UsedInSkus,
    DateTime? LastSapSyncAt);

public record ComponentMeasureDto(string? MeasuredBy, decimal? Value, string? Unit);

public record ComponentUnitOfMeasureDto(string? Type, decimal? Quantity, string? Unit);

public record ComponentUnitCostDto(decimal? Amount, string? Currency, string? Source);

public record ComponentFactoryDto(
    string FactoryId,
    decimal WarehouseStock,
    decimal ProductionFloorStock,
    decimal TotalStock,
    decimal? CriticalLevel,
    decimal? CriticalLevelOverride,
    StockStatus StockStatus,
    bool OosConflict,
    StockAlertStatus Status,
    ComponentManualOosDto ManualOos,
    DateTime? StockSyncedAt);

public record ComponentManualOosDto(bool Active, string? SetBy, DateTime? SetAt, string? Note);

public record ComponentAlternativeDto(
    string ErpId,
    string Name,
    ComponentStatus Status,
    bool ForBalancer,
    bool ForPlanner,
    IReadOnlyCollection<ComponentAlternativeStockDto> Factories);

public record ComponentAlternativeStockDto(string FactoryId, decimal TotalStock, StockAlertStatus Status);

public record ComponentSkuUsageDto(Guid SkuUuid, string Name, string SiteSku, decimal Quantity, ComponentUsage Usage);
