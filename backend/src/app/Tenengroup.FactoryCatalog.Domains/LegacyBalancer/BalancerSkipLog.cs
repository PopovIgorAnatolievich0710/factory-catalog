namespace Tenengroup.FactoryCatalog.Domains.LegacyBalancer;

public class BalancerSkipLog
{
    public long Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string? OrderItemId { get; set; }

    public Guid? SkuUuid { get; set; }

    public string FactoryId { get; set; } = string.Empty;

    public SkipReason Reason { get; set; }

    public string? ComponentErpId { get; set; }

    public string? ProcessId { get; set; }

    public DateTime CreatedAt { get; set; }
}
