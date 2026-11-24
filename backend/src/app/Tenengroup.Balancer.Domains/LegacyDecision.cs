namespace Tenengroup.Balancer.Domains;

public class LegacyDecision
{
    public long Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public decimal? TotalCost { get; set; }

    public DateOnly? Eta { get; set; }

    public DateTime ReceivedAt { get; set; }
}
