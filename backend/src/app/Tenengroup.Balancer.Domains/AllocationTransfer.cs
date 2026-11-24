namespace Tenengroup.Balancer.Domains;

public class AllocationTransfer
{
    public long Id { get; set; }

    public Guid AllocationId { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public string SkuUuid { get; set; } = string.Empty;

    public string FromFactoryId { get; set; } = string.Empty;

    public string? FromPage { get; set; }

    public string ToFactoryId { get; set; } = string.Empty;

    public string ToPage { get; set; } = string.Empty;

    public TransferReason Reason { get; set; }

    public string? Note { get; set; }

    public string? OldQuoteId { get; set; }

    public string? NewQuoteId { get; set; }

    public decimal? CostDelta { get; set; }

    public string? Currency { get; set; }

    public DateOnly? OldEta { get; set; }

    public DateOnly? NewEta { get; set; }

    public string TransferredBy { get; set; } = string.Empty;

    public TransferInitiator Initiator { get; set; }

    public DateTime TransferredAt { get; set; }
}
