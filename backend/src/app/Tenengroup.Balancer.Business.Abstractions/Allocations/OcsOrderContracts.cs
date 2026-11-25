using System.Text.Json.Serialization;
using Tenengroup.Balancer.Business.Abstractions.Catalog;

namespace Tenengroup.Balancer.Business.Abstractions.Allocations;

public record OcsPaidOrderMessage([property: JsonPropertyName("order")] OcsOrder? Order);

public record OcsOrder(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("om_id")] long OmId,
    [property: JsonPropertyName("site_id")] int SiteId,
    [property: JsonPropertyName("created_date")] DateTimeOffset CreatedDate,
    [property: JsonPropertyName("shipping_address")] OcsShippingAddress? ShippingAddress,
    [property: JsonPropertyName("fulfillments")] IReadOnlyCollection<OcsFulfillment>? Fulfillments,
    [property: JsonPropertyName("fulfillment_due_dates")] IReadOnlyCollection<OcsFulfillmentDueDates>? FulfillmentDueDates);

public record OcsShippingAddress([property: JsonPropertyName("country_code")] string? CountryCode);

public record OcsFulfillment(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("product_items")] IReadOnlyCollection<OcsProductItem>? ProductItems,
    [property: JsonPropertyName("estimated_delivery_date")] DateTimeOffset? EstimatedDeliveryDate,
    [property: JsonPropertyName("fulfillment_type")] string? FulfillmentType);

public record OcsProductItem(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("om_id")] long? OmId,
    [property: JsonPropertyName("sku")] string? Sku,
    [property: JsonPropertyName("user_selections")] string? UserSelections);

public record OcsFulfillmentDueDates(
    [property: JsonPropertyName("fulfillment_id")] Guid FulfillmentId,
    [property: JsonPropertyName("production_due_date")] DateOnly? ProductionDueDate,
    [property: JsonPropertyName("pickup_due_date")] DateOnly? PickupDueDate,
    [property: JsonPropertyName("is_guaranteed")] bool? IsGuaranteed);

public interface IOcsPaidOrderMapper
{
    Task<AllocateOrderCommand?> Map(OcsPaidOrderMessage message, CancellationToken cancellationToken);
}

public interface IComponentBreakdownClient
{
    Task<IReadOnlyCollection<CbdComponent>?> Get(int siteId, string userSelections, CancellationToken cancellationToken);
}
