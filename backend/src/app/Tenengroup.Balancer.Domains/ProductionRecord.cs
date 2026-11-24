namespace Tenengroup.Balancer.Domains;

public class ProductionRecord
{
    public long Id { get; set; }

    public Guid AllocationId { get; set; }

    public string SourceEventId { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string OrderItemId { get; set; } = string.Empty;

    public string SkuUuid { get; set; } = string.Empty;

    public string QuoteKey { get; set; } = string.Empty;

    public string QuoteId { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public string PackingFacilityId { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public int ProductionDays { get; set; }

    public bool IsReproduction { get; set; }

    public string? ReproductionOfQuoteId { get; set; }

    public long? ReproductionOfRecordId { get; set; }

    public string ProductionLogJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }
}
