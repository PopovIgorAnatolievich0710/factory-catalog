namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class SkuAlias
{
    public long Id { get; set; }

    public Guid SkuUuid { get; set; }

    public string Alias { get; set; } = string.Empty;

    public SkuAliasType Type { get; set; }

    public bool Deprecated { get; set; }

    public Sku? Sku { get; set; }
}
