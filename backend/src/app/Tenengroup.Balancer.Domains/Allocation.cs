namespace Tenengroup.Balancer.Domains;

public class Allocation
{
    public Guid Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public string SkuUuid { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public FulfillmentType FulfillmentType { get; set; }

    public string DestinationCountry { get; set; } = string.Empty;

    public DateOnly? PromisedDeliveryDate { get; set; }

    public DateOnly? SlaDeliveryDate { get; set; }

    public bool IsEventOrder { get; set; }

    public string? EventName { get; set; }

    public string? Site { get; set; }

    public DateTime? PaidAt { get; set; }

    public string CbdJson { get; set; } = "[]";

    public AllocationMode Mode { get; set; }

    public AllocationStatus Status { get; set; }

    public string? FallbackReason { get; set; }

    public string? QuoteKey { get; set; }

    public string? SelectedQuoteId { get; set; }

    public string? FactoryId { get; set; }

    public string? OriginalFactoryId { get; set; }

    public string? PackingFacilityId { get; set; }

    public string? ProductionPage { get; set; }

    public decimal? MaterialCost { get; set; }

    public decimal? LaborCost { get; set; }

    public decimal? PackingCost { get; set; }

    public decimal? ProductionCost { get; set; }

    public decimal? ShippingCost { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Currency { get; set; }

    public decimal? CheapestFeasibleTotalCost { get; set; }

    public decimal? ActualLaborCost { get; set; }

    public DateOnly? EtaFrom { get; set; }

    public DateOnly? EtaTo { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool? MeetsPromise { get; set; }

    public bool IsSplit { get; set; }

    public string DecisionReason { get; set; } = string.Empty;

    public string CandidatesJson { get; set; } = "[]";

    public int SettingsVersion { get; set; }

    public int TransferCount { get; set; }

    public DateTime DecidedAt { get; set; }

    public DateTime? OutputPublishedAt { get; set; }

    public DateTime? ProducedAt { get; set; }

    public DateTime? ReproductionRequestedAt { get; set; }

    public List<AllocationCostLine> CostLines { get; set; } = [];
}
