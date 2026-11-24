namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class SkuFactory
{
    public Guid SkuUuid { get; set; }

    public string FactoryId { get; set; } = string.Empty;

    public bool Producible { get; set; }

    public decimal? OverrideSharePct { get; set; }

    public string? OverrideBy { get; set; }

    public DateTime? OverrideAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Sku? Sku { get; set; }
}
