using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;

public record SapComponentRow(
    string ErpId,
    string Name,
    ComponentStatus Status,
    string? Family,
    string? Material,
    string? MeasuredBy,
    decimal? MeasureValue,
    string? MeasureUnit,
    string? UnitOfMeasureType,
    decimal? UnitOfMeasureQuantity,
    string? UnitOfMeasureUnit,
    string? CountryOfOrigin,
    string? HsCode,
    decimal? UnitCostAmount,
    string? UnitCostCurrency);
