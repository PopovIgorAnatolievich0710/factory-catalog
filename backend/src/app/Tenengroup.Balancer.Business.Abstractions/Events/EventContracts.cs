using Tenengroup.Balancer.Business.Abstractions.Catalog;

namespace Tenengroup.Balancer.Business.Abstractions.Events;

public static class BalancerEventTypes
{
    public const string AllocationDecided = "balancer.allocation.decided";
    public const string AllocationTransferred = "balancer.allocation.transferred";
    public const string QuoteProduced = "balancer.quote.produced";
    public const string OrderPaid = "paid_order_created_v2";
    public const string OcsEventTypeAttribute = "event_type";
    public const string SupplierItemSent = "TenenGroup.Supplier.Events.ItemSent";
    public const string SupplierItemProductionStatusChanged = "TenenGroup.Supplier.Events.ItemProductionStatusChanged";
    public const string SupplierReproductionStatus = "Reproduction";
    public const string SupplierEventTypeAttribute = "EventType";
}

public record Money(decimal Amount, string Currency);

public record EtaRange(DateOnly From, DateOnly To);

public record AllocationDecidedCandidate(string FactoryId, decimal? TotalCost, DateOnly? Eta, string? ExcludedReason);

public record AllocationDecidedEvent(
    string OrderId,
    string OrderItemId,
    string? QuoteKey,
    string? SelectedQuoteId,
    string FactoryId,
    string PackingFacilityId,
    Money? ProductionCost,
    Money? ShippingCost,
    Money? TotalCost,
    EtaRange? Eta,
    string Mode,
    IReadOnlyCollection<AllocationDecidedCandidate> Candidates);

public record AllocationTransferredEvent(
    string OrderId,
    string OrderItemId,
    string FromFactoryId,
    string? FromPage,
    string ToFactoryId,
    string ToPage,
    string? OldQuoteId,
    string? NewQuoteId,
    string Reason,
    string? Note,
    string TransferredBy,
    DateTime TransferredAt);

public record QuoteProducedEvent(
    string OrderId,
    string OrderItemId,
    string QuoteKey,
    string QuoteId,
    string Status,
    string FactoryId,
    string PackingFacilityId,
    QuoteCost? QuotedCost,
    DateTime SentAt,
    int ProductionDays,
    string? ReproductionOf);

public record SupplierItemSentMessage(long OrderId, long ItemId, int? SupplierId, string? SupplierType);

public record SupplierItemProductionStatusChangedMessage(
    long OrderId,
    long ItemId,
    string? SupplierItemId,
    string? ProductionStatus,
    DateTime ChangedAt);
