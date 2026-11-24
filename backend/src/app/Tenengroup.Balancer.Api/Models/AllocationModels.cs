using System.ComponentModel.DataAnnotations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Api.Models;

public class AllocateOrderModel
{
    public const int CatalogIdMaxLength = 90;

    [Required]
    [MaxLength(CatalogIdMaxLength)]
    public string OrderId { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string DestinationCountry { get; set; }

    public DateOnly? PromisedDeliveryDate { get; set; }

    public DateOnly? SlaDeliveryDate { get; set; }

    public bool IsEventOrder { get; set; }

    [MaxLength(200)]
    public string EventName { get; set; }

    [MaxLength(100)]
    public string Site { get; set; }

    public DateTime? PaidAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<AllocateOrderItemModel> Items { get; set; }
}

public class AllocateOrderItemModel
{
    [Required]
    [MaxLength(AllocateOrderModel.CatalogIdMaxLength)]
    public string OrderItemId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SkuUuid { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public List<CbdComponent> Cbd { get; set; }

    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Factory;
}

public class EstimateModel
{
    [Required]
    [MaxLength(100)]
    public string SkuUuid { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public List<CbdComponent> Cbd { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string DestinationCountry { get; set; }

    public DateOnly? PromisedDeliveryDate { get; set; }
}

public class LegacyDecisionModel
{
    [Required]
    [MaxLength(100)]
    public string OrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderItemId { get; set; }

    [Required]
    [MaxLength(20)]
    public string FactoryId { get; set; }

    public decimal? TotalCost { get; set; }

    public DateOnly? Eta { get; set; }
}

public class CapacityReservationModel
{
    [Required]
    [MaxLength(20)]
    public string FactoryId { get; set; }

    [Required]
    public DateOnly? DateFrom { get; set; }

    [Required]
    public DateOnly? DateTo { get; set; }

    [Range(1, 100)]
    public int ReservedPct { get; set; }

    [Required]
    [MaxLength(200)]
    public string EventName { get; set; }
}

public class DashboardFilterModel
{
    public string FactoryId { get; set; }

    public string Country { get; set; }

    [Required]
    public DateOnly? From { get; set; }

    [Required]
    public DateOnly? To { get; set; }
}
