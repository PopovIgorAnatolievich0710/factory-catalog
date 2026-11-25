namespace Tenengroup.Balancer.Domains;

public class ShippingRoute
{
    public long Id { get; set; }

    public string OriginFacilityId { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public decimal CostAmount { get; set; }

    public string CostCurrency { get; set; } = string.Empty;

    public int TransitDaysMin { get; set; }

    public int TransitDaysMax { get; set; }

    public int[] WorkingDays { get; set; } = [];

    public DateTime RefreshedAt { get; set; }
}
