using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Balancer.Business.Shipping;
using static Tenengroup.Balancer.Business.UnitTests.TestData;

namespace Tenengroup.Balancer.Business.UnitTests.Shipping;

[TestFixture]
public class ShippingRouteTests
{
    private const string RoutesPage = """
        {
          "data": [
            { "id": 1, "packingFacilityIds": [1], "toCountryCodes": ["de"], "shippingCost": 7.9, "currencyCode": "USD",
              "packingTimeFrame": 1, "shippingTimeFrame": 3, "status": 0, "priority": false,
              "shippingWorkingDays": [1, 2, 3, 4, 5], "carrierServiceType": { "method": 0 } },
            { "id": 2, "packingFacilityIds": [1], "toCountryCodes": ["DE"], "shippingCost": 30, "currencyCode": "USD",
              "packingTimeFrame": 1, "shippingTimeFrame": 1, "status": 0, "priority": false,
              "shippingWorkingDays": [1, 2, 3, 4, 5], "carrierServiceType": { "method": 2 } },
            { "id": 3, "packingFacilityIds": null, "toCountryCodes": ["GB"], "shippingCost": 6.1, "currencyCode": "USD",
              "packingTimeFrame": 1, "shippingTimeFrame": 2, "status": 0, "priority": false,
              "shippingWorkingDays": [], "carrierServiceType": { "method": 0 } },
            { "id": 4, "packingFacilityIds": [7], "toCountryCodes": ["DE"], "shippingCost": 5, "currencyCode": "USD",
              "packingTimeFrame": 1, "shippingTimeFrame": 4, "status": 1, "priority": false,
              "shippingWorkingDays": [1, 2, 3, 4, 5], "carrierServiceType": { "method": 0 } }
          ],
          "totalRowCount": 4
        }
        """;

    private static BalancerOptions Options() => new()
    {
        PackingFacilities = new Dictionary<string, string> { ["110"] = "1", ["125"] = "7" }
    };

    [Test]
    public async Task Client_keeps_active_routes_of_the_service_level_and_expands_routes_open_to_every_facility()
    {
        var client = new ShippingRouteClient(
            new HttpClient(new StubHandler(RoutesPage)) { BaseAddress = new Uri("http://shipment/") },
            Options());

        var routes = await client.GetRoutes(CancellationToken.None);

        Assert.That(routes.Select(r => $"{r.OriginFacilityId}-{r.DestinationCountry}-{r.CostAmount}"),
            Is.EquivalentTo(new[] { "1-DE-7.9", "1-GB-6.1", "7-GB-6.1" }));
        var germany = routes.Single(r => r.DestinationCountry == "DE");
        Assert.That(germany.TransitDaysMin, Is.EqualTo(3));
        Assert.That(germany.TransitDaysMax, Is.EqualTo(4));
        Assert.That(germany.WorkingDays, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        Assert.That(routes.First(r => r.DestinationCountry == "GB").WorkingDays, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task Refresh_prefers_a_priority_route_over_a_cheaper_one()
    {
        var db = EmptyDb();
        var client = new Mock<IShippingRouteClient>();
        client.Setup(c => c.GetRoutes(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ShippingRouteDto("1", "DE", 5m, "USD", 3, 4, false, [1, 2, 3, 4, 5]),
            new ShippingRouteDto("1", "DE", 9m, "USD", 2, 3, true, [1, 2, 3, 4, 5])
        ]);

        await new ShippingRouteRefreshJob(db.Object, client.Object, FixedTime(), NullLogger<ShippingRouteRefreshJob>.Instance)
            .Run(CancellationToken.None);

        var cached = db.Object.ShippingRoutes.Single();
        Assert.That(cached.CostAmount, Is.EqualTo(9m));
        Assert.That(cached.WorkingDays, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    private class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
