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

    public string EventsTopicArn { get; set; } = string.Empty;

    public string OrderQueueUrl { get; set; } = string.Empty;

    public string SupplierQueueUrl { get; set; } = string.Empty;

    public int QueuePollSeconds { get; set; } = 5;

    public string ShippingRoutesCronExpression { get; set; } = "0 1 * * *";
}
