namespace Tenengroup.FactoryCatalog.Domains.Skus;

public record FactoryShareInput(string FactoryId, decimal DefaultPct, decimal? OverridePct, bool CanProduce);
