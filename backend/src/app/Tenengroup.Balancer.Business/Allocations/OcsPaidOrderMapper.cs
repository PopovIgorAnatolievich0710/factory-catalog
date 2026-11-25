using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Business.Allocations;

public class OcsPaidOrderMapper(
    IComponentBreakdownClient componentBreakdownClient,
    ILogger<OcsPaidOrderMapper> logger) : IOcsPaidOrderMapper
{
    private const int ItemQuantity = 1;

    public async Task<AllocateOrderCommand?> Map(OcsPaidOrderMessage message, CancellationToken cancellationToken)
    {
        var order = message.Order;
        var country = order?.ShippingAddress?.CountryCode;
        if (order == null || string.IsNullOrWhiteSpace(country))
        {
            logger.LogError("Paid order {OrderId} has no destination country and is not allocated", order?.Id);
            return null;
        }

        var fulfillments = order.Fulfillments ?? [];
        var items = new List<AllocationItemRequest>();

        foreach (var fulfillment in fulfillments)
        {
            var fulfillmentType = ToFulfillmentType(fulfillment.FulfillmentType);

            foreach (var item in fulfillment.ProductItems ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Sku))
                {
                    logger.LogWarning("Paid order {OrderId} item {ItemId} has no SKU and is skipped", order.Id, item.Id);
                    continue;
                }

                var cbd = fulfillmentType == FulfillmentType.Factory && !string.IsNullOrWhiteSpace(item.UserSelections)
                    ? await componentBreakdownClient.Get(order.SiteId, item.UserSelections, cancellationToken)
                    : null;

                items.Add(new AllocationItemRequest(
                    item.OmId is > 0 ? item.OmId.Value.ToString() : item.Id.ToString(),
                    item.Sku,
                    ItemQuantity,
                    cbd,
                    fulfillmentType));
            }
        }

        if (items.Count == 0)
        {
            logger.LogWarning("Paid order {OrderId} has no product items to allocate", order.Id);
            return null;
        }

        var promised = fulfillments
            .Where(f => ToFulfillmentType(f.FulfillmentType) == FulfillmentType.Factory && f.EstimatedDeliveryDate.HasValue)
            .Select(f => DateOnly.FromDateTime(f.EstimatedDeliveryDate.Value.UtcDateTime))
            .DefaultIfEmpty()
            .Min();

        return new AllocateOrderCommand(
            order.OmId > 0 ? order.OmId.ToString() : order.Id.ToString(),
            country.ToUpperInvariant(),
            promised == default ? null : promised,
            null,
            false,
            null,
            order.SiteId.ToString(),
            order.CreatedDate.UtcDateTime,
            items);
    }

    private static FulfillmentType ToFulfillmentType(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "virtual" => FulfillmentType.Virtual,
            "dropshipper" => FulfillmentType.Dropshipper,
            _ => FulfillmentType.Factory
        };
    }
}
