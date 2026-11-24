namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

public record MasterCatalogSkuPage(IReadOnlyCollection<MasterCatalogSku>? Items, bool HasMore);

public record MasterCatalogSku(
    Guid SkuUuid,
    string? SiteSku,
    string? Name,
    string? ProductType,
    string? Status,
    IReadOnlyCollection<MasterCatalogSkuAlias>? Aliases,
    IReadOnlyCollection<MasterCatalogSkuComponent>? Components,
    MasterCatalogDropshipper? Dropshipper);

public record MasterCatalogSkuAlias(string? Id, string? Type, bool Deprecated);

public record MasterCatalogSkuComponent(string? ErpId, decimal Quantity, string? Usage);

public record MasterCatalogDropshipper(string? DropshipperId, string? Name, MasterCatalogMoney? TotalCost);

public record MasterCatalogMoney(decimal Amount, string? Currency);
