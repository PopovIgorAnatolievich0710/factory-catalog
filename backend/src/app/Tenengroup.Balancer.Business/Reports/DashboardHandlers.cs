using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.Business.Abstractions.Reports;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Reports;

internal static class DashboardQuery
{
    public static IQueryable<Allocation> Routed(IBalancerDbContext db, DashboardFilter filter)
    {
        if (filter.To < filter.From)
        {
            throw new ValidationException("to", "The end date is before the start date.");
        }

        var from = filter.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = filter.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return db.Allocations
            .AsNoTracking()
            .Where(a => a.FactoryId != null && a.DecidedAt >= from && a.DecidedAt < to);
    }
}

public class GetRoutingDashboardQueryHandler(IBalancerDbContext db, IBalancerSettingsProvider settingsProvider)
    : IRequestHandler<GetRoutingDashboardQuery, RoutingDashboardDto>
{
    public async Task<RoutingDashboardDto> Handle(GetRoutingDashboardQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var settings = await settingsProvider.GetCurrent(cancellationToken);
        var inPeriod = await DashboardQuery.Routed(db, filter).ToListAsync(cancellationToken);

        var countries = inPeriod.Select(a => a.DestinationCountry).Distinct().OrderBy(c => c).ToList();
        var inCountry = string.IsNullOrEmpty(filter.Country)
            ? inPeriod
            : inPeriod.Where(a => a.DestinationCountry == filter.Country.ToUpperInvariant()).ToList();
        var rows = string.IsNullOrEmpty(filter.FactoryId)
            ? inCountry
            : inCountry.Where(a => a.FactoryId == filter.FactoryId).ToList();

        var transferred = rows.Count(a => a.TransferCount > 0);
        var produced = rows.Where(a => a.ProducedAt.HasValue && a.DueDate.HasValue).ToList();
        var costed = rows.Where(a => a.TotalCost.HasValue && a.CheapestFeasibleTotalCost > 0).ToList();

        return new RoutingDashboardDto(
            rows.Count,
            rows.Select(a => a.OrderId).Distinct().Count(),
            transferred,
            rows.Count > 0 ? Math.Round(100m * transferred / rows.Count, 1) : null,
            produced.Count > 0
                ? Math.Round(100m * produced.Count(a => DateOnly.FromDateTime(a.ProducedAt.Value) <= a.DueDate.Value) / produced.Count, 1)
                : null,
            costed.Count > 0
                ? Math.Round(costed.Average(a => (a.TotalCost.Value - a.CheapestFeasibleTotalCost.Value) / a.CheapestFeasibleTotalCost.Value * 100m), 2)
                : null,
            rows.Count(a => a.FallbackReason != null),
            settings.ShareGapThresholdPct,
            rows
                .GroupBy(a => (Date: DateOnly.FromDateTime(a.DecidedAt), a.FactoryId))
                .Select(g => new RoutedPerDayDto(g.Key.Date, g.Key.FactoryId, g.Count()))
                .OrderBy(d => d.Date)
                .ThenBy(d => d.FactoryId)
                .ToList(),
            Shares(inCountry, settings.ShareGapThresholdPct),
            countries);
    }

    private static List<FactoryShareDto> Shares(List<Allocation> rows, decimal gapThreshold)
    {
        var targets = rows
            .Select(a => a.ReadCandidates())
            .Where(candidates => candidates.Any(c => c.SharePct > 0))
            .ToList();

        var targetByFactory = targets
            .SelectMany(candidates => candidates)
            .GroupBy(c => c.FactoryId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.SharePct) / targets.Count);

        var actualByFactory = rows
            .GroupBy(a => a.FactoryId)
            .ToDictionary(g => g.Key, g => 100m * g.Count() / rows.Count);

        return targetByFactory.Keys
            .Union(actualByFactory.Keys)
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(factoryId =>
            {
                var target = Math.Round(targetByFactory.GetValueOrDefault(factoryId), 1);
                var actual = Math.Round(actualByFactory.GetValueOrDefault(factoryId), 1);
                var gap = actual - target;
                return new FactoryShareDto(factoryId, target, actual, gap, Math.Abs(gap) > gapThreshold);
            })
            .ToList();
    }
}

public class GetShadowComparisonQueryHandler(IBalancerDbContext db)
    : IRequestHandler<GetShadowComparisonQuery, ShadowComparisonDto>
{
    public async Task<ShadowComparisonDto> Handle(GetShadowComparisonQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var shadow = await DashboardQuery.Routed(db, filter)
            .Where(a => a.Mode == AllocationMode.Shadow)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrEmpty(filter.Country))
        {
            shadow = shadow.Where(a => a.DestinationCountry == filter.Country.ToUpperInvariant()).ToList();
        }

        var orderIds = shadow.Select(a => a.OrderId).Distinct().ToList();
        var legacy = await db.LegacyDecisions
            .AsNoTracking()
            .Where(d => orderIds.Contains(d.OrderId))
            .ToListAsync(cancellationToken);

        var compared = shadow
            .Join(legacy, a => (a.OrderId, a.OrderItemId), d => (d.OrderId, d.OrderItemId), (a, d) => (Allocation: a, Legacy: d))
            .Where(pair => string.IsNullOrEmpty(filter.FactoryId)
                           || pair.Allocation.FactoryId == filter.FactoryId
                           || pair.Legacy.FactoryId == filter.FactoryId)
            .ToList();

        var disagreed = compared.Where(pair => pair.Allocation.FactoryId != pair.Legacy.FactoryId).ToList();
        var savings = 0m;
        var reasons = new List<string>();

        foreach (var (allocation, decision) in disagreed)
        {
            var legacyCandidate = allocation.ReadCandidates().FirstOrDefault(c => c.FactoryId == decision.FactoryId);
            var legacyCost = decision.TotalCost ?? legacyCandidate?.TotalCost;

            if (legacyCost.HasValue && allocation.TotalCost.HasValue)
            {
                savings += legacyCost.Value - allocation.TotalCost.Value;
            }

            reasons.Add(legacyCandidate switch
            {
                null => "Legacy factory was not quoted",
                { ExcludedReason: not null } => $"Legacy factory excluded: {legacyCandidate.ExcludedReason}",
                { MeetsPromise: false } when allocation.MeetsPromise == true => "Legacy pick misses the delivery promise",
                _ when legacyCost > allocation.TotalCost => "Lower total cost",
                _ => "Share, capacity or order-level rules"
            });
        }

        return new ShadowComparisonDto(
            compared.Count,
            compared.Count - disagreed.Count,
            compared.Count > 0 ? Math.Round(100m * (compared.Count - disagreed.Count) / compared.Count, 1) : null,
            Math.Round(savings, 2),
            shadow.Select(a => a.Currency).FirstOrDefault(c => c != null),
            reasons
                .GroupBy(r => r)
                .Select(g => new DisagreementReasonDto(g.Key, g.Count(), Math.Round(100m * g.Count() / reasons.Count, 1)))
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.Reason)
                .ToList());
    }
}
