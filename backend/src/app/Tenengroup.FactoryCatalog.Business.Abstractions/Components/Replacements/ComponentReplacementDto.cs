namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;

public record ComponentReplacementDto(
    long Id,
    string OrderId,
    string OrderItemId,
    string FactoryId,
    string OriginalErpId,
    string ReplacementErpId,
    decimal? Quantity,
    string ReplacedBy,
    DateTime ReplacedAt);
