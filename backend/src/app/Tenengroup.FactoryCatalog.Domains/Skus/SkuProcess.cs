namespace Tenengroup.FactoryCatalog.Domains.Skus;

public class SkuProcess
{
    public Guid SkuUuid { get; set; }

    public string ProcessId { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public Sku? Sku { get; set; }
}
