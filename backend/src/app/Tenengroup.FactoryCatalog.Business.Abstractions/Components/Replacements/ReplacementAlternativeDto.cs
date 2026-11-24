using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;

public record ReplacementAlternativeDto(
    string ErpId,
    string Name,
    string? UnitOfMeasureUnit,
    decimal WarehouseStock,
    decimal ProductionFloorStock,
    decimal TotalStock,
    StockStatus StockStatus);
