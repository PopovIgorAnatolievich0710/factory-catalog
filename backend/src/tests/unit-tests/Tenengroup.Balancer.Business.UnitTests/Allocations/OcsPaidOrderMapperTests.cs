using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.UnitTests.Allocations;

[TestFixture]
public class OcsPaidOrderMapperTests
{
    private const string PaidOrderJson = """
        {
          "order": {
            "id": "6f1c1f3e-8f0f-4c7b-9a51-1f2d3c4b5a69",
            "om_id": 900123,
            "site_id": 7,
            "status": 1,
            "created_date": "2026-11-25T09:30:00+00:00",
            "currency_symbol": "$",
            "shipping_address": { "country_code": "de", "country_name": "Germany" },
            "fulfillments": [
              {
                "id": "0b0d8f63-8d7e-4a7d-8a3a-5f0c2d2e1a11",
                "estimated_delivery_date": "2026-12-08T00:00:00+00:00",
                "status": 0,
                "fulfillment_type": "tg_shipment",
                "product_items": [
                  { "id": "c7e1d7a2-1111-4b4b-9c9c-000000000001", "om_id": 5001, "sku": "NCK-001", "status": 0, "user_selections": "{\"a\":1}" },
                  { "id": "c7e1d7a2-1111-4b4b-9c9c-000000000002", "om_id": 5002, "sku": "NCK-001", "status": 0, "user_selections": "{\"a\":1}" }
                ]
              },
              {
                "id": "0b0d8f63-8d7e-4a7d-8a3a-5f0c2d2e1a12",
                "estimated_delivery_date": "2026-12-20T00:00:00+00:00",
                "status": 0,
                "fulfillment_type": "dropshipper",
                "product_items": [
                  { "id": "c7e1d7a2-1111-4b4b-9c9c-000000000003", "om_id": null, "sku": "DS-77", "status": 0, "user_selections": "{}" }
                ]
              }
            ],
            "fulfillment_due_dates": []
          }
        }
        """;

    private Mock<IComponentBreakdownClient> breakdown;

    [SetUp]
    public void SetUp()
    {
        breakdown = new Mock<IComponentBreakdownClient>();
        breakdown
            .Setup(b => b.Get(7, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CbdComponent("CHN-1", 1m)]);
    }

    private OcsPaidOrderMapper Mapper() => new(breakdown.Object, NullLogger<OcsPaidOrderMapper>.Instance);

    [Test]
    public async Task Paid_order_v2_becomes_one_single_unit_item_per_product_item_keyed_by_om_ids()
    {
        var command = await Mapper().Map(PaidOrderJson.FromJsonString<OcsPaidOrderMessage>(), CancellationToken.None);

        Assert.That(command, Is.Not.Null);
        Assert.That(command.OrderId, Is.EqualTo("900123"));
        Assert.That(command.DestinationCountry, Is.EqualTo("DE"));
        Assert.That(command.Site, Is.EqualTo("7"));
        Assert.That(command.PaidAt, Is.EqualTo(new DateTime(2026, 11, 25, 9, 30, 0, DateTimeKind.Utc)));
        Assert.That(command.PromisedDeliveryDate, Is.EqualTo(new DateOnly(2026, 12, 8)));
        Assert.That(command.SlaDeliveryDate, Is.Null);
        Assert.That(command.IsEventOrder, Is.False);

        Assert.That(command.Items.Select(i => i.OrderItemId), Is.EqualTo(new[] { "5001", "5002", "c7e1d7a2-1111-4b4b-9c9c-000000000003" }));
        Assert.That(command.Items.All(i => i.Quantity == 1), Is.True);
        Assert.That(command.Items.First().SkuUuid, Is.EqualTo("NCK-001"));
        Assert.That(command.Items.First().FulfillmentType, Is.EqualTo(FulfillmentType.Factory));
        Assert.That(command.Items.First().Cbd.Single().ErpId, Is.EqualTo("CHN-1"));
        Assert.That(command.Items.Last().FulfillmentType, Is.EqualTo(FulfillmentType.Dropshipper));
        Assert.That(command.Items.Last().Cbd, Is.Null);
        breakdown.Verify(b => b.Get(7, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task An_order_without_a_destination_country_is_not_allocated()
    {
        var command = await Mapper().Map(
            new OcsPaidOrderMessage(new OcsOrder(Guid.NewGuid(), 1, 7, DateTimeOffset.UtcNow, null, [], [])),
            CancellationToken.None);

        Assert.That(command, Is.Null);
    }
}
