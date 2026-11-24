namespace Tenengroup.Balancer.Domains;

public class CapacityReservation
{
    public long Id { get; set; }

    public string FactoryId { get; set; } = string.Empty;

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public int ReservedPct { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
