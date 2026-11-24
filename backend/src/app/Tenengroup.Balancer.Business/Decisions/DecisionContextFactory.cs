using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;

namespace Tenengroup.Balancer.Business.Decisions;

public record DecisionContextRequest(
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? SlaDeliveryDate,
    bool IsEventOrder,
    IReadOnlyCollection<DecisionItem> Items,
    IReadOnlyCollection<string> AnchorFactoryIds);

public interface IDecisionContextFactory
{
    Task<DecisionInput> Create(DecisionContextRequest request, BalancerSettings settings, CancellationToken cancellationToken);
}

public class DecisionContextFactory(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    BalancerOptions options,
    TimeProvider timeProvider,
    ILogger<DecisionContextFactory> logger) : IDecisionContextFactory
{
    private const int ProductionHistoryDays = 90;

    public async Task<DecisionInput> Create(DecisionContextRequest request, BalancerSettings settings, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var country = request.DestinationCountry.ToUpperInvariant();

        var routes = await db.ShippingRoutes
            .AsNoTracking()
            .Where(r => r.DestinationCountry == country)
            .ToListAsync(cancellationToken);

        var reservations = await db.CapacityReservations
            .AsNoTracking()
            .Where(r => r.DateFrom <= today && r.DateTo >= today)
            .ToListAsync(cancellationToken);

        var isShadow = settings.Mode == BalancerMode.Shadow;
        var shareWindowStart = now.AddDays(-settings.ShareWindowDays);
        var recentFactories = await db.Allocations
            .AsNoTracking()
            .Where(a => a.DecidedAt >= shareWindowStart && a.FactoryId != null && (a.Mode == AllocationMode.Shadow) == isShadow)
            .Select(a => a.FactoryId)
            .ToListAsync(cancellationToken);

        var historyStart = now.AddDays(-ProductionHistoryDays);
        var production = await db.ProductionRecords
            .AsNoTracking()
            .Where(r => r.SentAt >= historyStart)
            .Select(r => new { r.FactoryId, r.ProductionDays, r.IsReproduction })
            .ToListAsync(cancellationToken);

        return new DecisionInput(
            request.Items,
            today,
            request.PromisedDeliveryDate,
            request.SlaDeliveryDate,
            request.IsEventOrder,
            routes.Select(r => new RouteOption(r.OriginFacilityId, r.CostAmount, r.CostCurrency, r.TransitDaysMin, r.TransitDaysMax)).ToList(),
            reservations
                .GroupBy(r => r.FactoryId)
                .ToDictionary(g => g.Key, g => Math.Min(100, g.Sum(r => r.ReservedPct))),
            recentFactories
                .GroupBy(f => f)
                .ToDictionary(g => g.Key, g => 100m * g.Count() / recentFactories.Count),
            await GetFallbackShares(cancellationToken),
            production
                .Where(r => !r.IsReproduction)
                .GroupBy(r => r.FactoryId)
                .ToDictionary(g => g.Key, g => (int)Math.Ceiling(g.Average(r => r.ProductionDays))),
            production
                .GroupBy(r => r.FactoryId)
                .Where(g => g.Any(r => !r.IsReproduction))
                .ToDictionary(
                    g => g.Key,
                    g => Math.Max(0m, 1m - (decimal)g.Count(r => r.IsReproduction) / g.Count(r => !r.IsReproduction))),
            request.AnchorFactoryIds,
            new DecisionSettings(
                settings.LatePenaltyPerDay,
                settings.SplitPenalty,
                settings.ShareWeight,
                settings.SuccessRateEnabled,
                settings.SuccessRateWeight,
                settings.DefaultProductionDays));
    }

    private async Task<IReadOnlyDictionary<string, decimal>> GetFallbackShares(CancellationToken cancellationToken)
    {
        if (options.FallbackShares.Count > 0)
        {
            return options.FallbackShares;
        }

        try
        {
            var factories = (await catalogClient.GetFactories(cancellationToken)).Where(f => f.IsActive).ToList();
            return factories.ToDictionary(f => f.Id, _ => 100m / factories.Count);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Fallback factory shares are not configured and the factory list could not be read from the catalog");
            return new Dictionary<string, decimal>();
        }
    }
}
