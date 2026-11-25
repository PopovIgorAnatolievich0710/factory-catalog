using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Business.Abstractions;

public class BalancerOptions
{
    public const string SectionName = "Balancer";

    public BalancerMode Mode { get; set; } = BalancerMode.Shadow;

    public decimal LatePenaltyPerDay { get; set; } = 5m;

    public decimal SplitPenalty { get; set; } = 3m;

    public decimal ShareWeight { get; set; } = 0.1m;

    public bool SuccessRateEnabled { get; set; }

    public decimal SuccessRateWeight { get; set; } = 0.5m;

    public int ShareWindowDays { get; set; } = 7;

    public decimal ShareGapThresholdPct { get; set; } = 2m;

    public int DefaultProductionDays { get; set; } = 3;

    public Dictionary<string, decimal> FallbackShares { get; set; } = new();

    public Dictionary<string, string[]> ProductionPages { get; set; } = new();

    public int QueuePollSeconds { get; set; } = 5;

    public string ShippingRoutesCronExpression { get; set; } = "0 1 * * *";

    public Dictionary<string, string> PackingFacilities { get; set; } = new();

    public int ShippingServiceLevel { get; set; }

    public string? PackingFacilityFor(string factoryId)
    {
        return PackingFacilities.TryGetValue(factoryId, out var facilityId) ? facilityId : null;
    }
}

public class MasterCatalogOptions
{
    public const string SectionName = "MasterCatalog";

    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class OrderItemsOptions
{
    public const string SectionName = "OrderItems";

    public string QueueUrl { get; set; } = string.Empty;
}

public class SupplierEventsOptions
{
    public const string SectionName = "SupplierEvents";

    public string QueueUrl { get; set; } = string.Empty;
}

public class BalancerEventsOptions
{
    public const string SectionName = "BalancerEvents";

    public string AllocationDecidedTopicArn { get; set; } = string.Empty;

    public string QuoteProducedTopicArn { get; set; } = string.Empty;
}
