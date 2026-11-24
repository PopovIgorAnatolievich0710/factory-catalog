namespace Tenengroup.FactoryCatalog.Domains.Components;

public class ComponentReplacement
{
    public long Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public string OriginalErpId { get; set; } = string.Empty;

    public string ReplacementErpId { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string ReplacedBy { get; set; } = string.Empty;

    public DateTime ReplacedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
