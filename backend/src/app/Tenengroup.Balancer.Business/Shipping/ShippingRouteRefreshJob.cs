using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Business.Shipping;

public class ShippingRouteRefreshJob(
    IBalancerDbContext db,
    IShippingRouteClient shippingRouteClient,
    TimeProvider timeProvider,
    ILogger<ShippingRouteRefreshJob> logger) : IShippingRouteRefreshJob
{
    public async Task Run(CancellationToken cancellationToken)
    {
        var fetched = await shippingRouteClient.GetRoutes(cancellationToken);
        if (fetched.Count == 0)
        {
            logger.LogError("Shipment service returned no active shipping routes; the cached routes are kept");
            return;
        }

        var cheapest = fetched
            .GroupBy(r => (r.OriginFacilityId, r.DestinationCountry))
            .Select(g => g.OrderBy(r => r.CostAmount).ThenBy(r => r.TransitDaysMax).First())
            .ToList();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cached = await db.ShippingRoutes.ToListAsync(cancellationToken);

        foreach (var route in cheapest)
        {
            var row = cached.FirstOrDefault(c =>
                c.OriginFacilityId == route.OriginFacilityId && c.DestinationCountry == route.DestinationCountry);

            if (row == null)
            {
                row = new ShippingRoute
                {
                    OriginFacilityId = route.OriginFacilityId,
                    DestinationCountry = route.DestinationCountry
                };
                db.ShippingRoutes.Add(row);
            }

            row.CostAmount = route.CostAmount;
            row.CostCurrency = route.CostCurrency;
            row.TransitDaysMin = route.TransitDaysMin;
            row.TransitDaysMax = route.TransitDaysMax;
            row.RefreshedAt = now;
        }

        var removed = cached
            .Where(c => !cheapest.Any(r =>
                r.OriginFacilityId == c.OriginFacilityId && r.DestinationCountry == c.DestinationCountry))
            .ToList();
        db.ShippingRoutes.RemoveRange(removed);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Shipping route cache refreshed: {Count} routes, {Removed} removed", cheapest.Count, removed.Count);
    }
}
