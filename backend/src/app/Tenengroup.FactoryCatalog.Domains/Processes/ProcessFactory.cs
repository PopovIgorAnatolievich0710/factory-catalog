namespace Tenengroup.FactoryCatalog.Domains.Processes;

public class ProcessFactory
{
    public const int FullCapacityPct = 100;
    public const int CapacityStepPct = 10;

    public string ProcessId { get; set; } = string.Empty;

    public string FactoryId { get; set; } = string.Empty;

    public bool RunsHere { get; set; }

    public string? MachineType { get; set; }

    public int CapacityPct { get; set; } = FullCapacityPct;

    public string? CapacityReason { get; set; }

    public DateOnly? ExpectedRecovery { get; set; }

    public string? CapacityUpdatedBy { get; set; }

    public DateTime? CapacityUpdatedAt { get; set; }

    public decimal? MaxPerDay { get; set; }

    public decimal? LaborCostAmount { get; set; }

    public string? LaborCostCurrency { get; set; }

    public DateOnly? LaborCostEffectiveFrom { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Process? Process { get; set; }
}
