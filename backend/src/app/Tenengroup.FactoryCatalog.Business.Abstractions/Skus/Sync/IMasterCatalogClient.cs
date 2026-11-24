namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;

public interface IMasterCatalogClient
{
    Task<MasterCatalogSkuPage> GetChangedSkus(DateTime? changedSince, int page, int pageSize, CancellationToken cancellationToken);
}
