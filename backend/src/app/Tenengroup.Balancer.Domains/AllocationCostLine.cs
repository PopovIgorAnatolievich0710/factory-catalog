namespace Tenengroup.Balancer.Domains;

public class AllocationCostLine
{
    public long Id { get; set; }

    public Guid AllocationId { get; set; }

    public CostLineKind Kind { get; set; }

    public string Code { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal? QuotedUnitCost { get; set; }

    public decimal QuotedCost { get; set; }

    public decimal? ActualQuantity { get; set; }

    public decimal? ActualCost { get; set; }

    public Allocation? Allocation { get; set; }
}
