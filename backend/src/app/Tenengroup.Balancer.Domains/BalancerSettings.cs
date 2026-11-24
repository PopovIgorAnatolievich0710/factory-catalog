namespace Tenengroup.Balancer.Domains;

public class BalancerSettings
{
    public int Version { get; set; }

    public BalancerMode Mode { get; set; }

    public decimal LatePenaltyPerDay { get; set; }

    public decimal SplitPenalty { get; set; }

    public decimal ShareWeight { get; set; }

    public bool SuccessRateEnabled { get; set; }

    public decimal SuccessRateWeight { get; set; }

    public int ShareWindowDays { get; set; }

    public decimal ShareGapThresholdPct { get; set; }

    public int DefaultProductionDays { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
