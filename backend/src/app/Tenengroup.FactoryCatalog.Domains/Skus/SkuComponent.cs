namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class SkuComponent
{
    public Guid SkuUuid { get; set; }

    public string ComponentErpId { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public ComponentUsage Usage { get; set; }

    public Sku? Sku { get; set; }
}
