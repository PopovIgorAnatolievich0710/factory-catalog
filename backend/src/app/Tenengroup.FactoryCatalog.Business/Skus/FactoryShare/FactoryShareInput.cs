namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public record FactoryShareInput(string FactoryId, decimal DefaultPct, decimal? OverridePct, bool CanProduce);
