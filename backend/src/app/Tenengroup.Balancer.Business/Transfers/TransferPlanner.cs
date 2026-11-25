using Tenengroup.Balancer.Business.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Abstractions.Transfers;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.Transfers;

public record PlannedTransfer(Allocation Allocation, TransferItemDto Item, QuoteSet QuoteSet, FactoryQuote TargetQuote, decimal? ShippingCost)
{
    public bool IsExecutable => !Item.Skipped && Item.Problem == null;
}

public interface ITransferPlanner
{
    Task<IReadOnlyCollection<PlannedTransfer>> Plan(TransferRequest request, string quoteMode, CancellationToken cancellationToken);
}

public class TransferPlanner(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    ICurrentUser currentUser,
    IBalancerSettingsProvider settingsProvider,
    BalancerOptions options,
    TimeProvider timeProvider,
    ILogger<TransferPlanner> logger) : ITransferPlanner
{
    public async Task<IReadOnlyCollection<PlannedTransfer>> Plan(TransferRequest request, string quoteMode, CancellationToken cancellationToken)
    {
        Validate(request);

        var allocations = await db.Allocations
            .Where(a => request.AllocationIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        foreach (var factoryId in allocations.Where(a => a.FactoryId != null).Select(a => a.FactoryId).Distinct())
        {
            await currentUser.EnsureCanAccessFactory(factoryId, cancellationToken);
        }

        var settings = await settingsProvider.GetCurrent(cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var routes = await db.ShippingRoutes
            .AsNoTracking()
            .Where(r => r.OriginFacilityId == options.PackingFacilityFor(request.ToFactoryId))
            .ToListAsync(cancellationToken);

        var planned = new List<PlannedTransfer>();
        foreach (var allocation in allocations.OrderBy(a => a.OrderId).ThenBy(a => a.OrderItemId))
        {
            planned.Add(await PlanItem(allocation, request, quoteMode, routes, settings, today, cancellationToken));
        }

        return planned;
    }

    private static void Validate(TransferRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (request.AllocationIds == null || request.AllocationIds.Count == 0)
        {
            errors["allocationIds"] = ["Select at least one item."];
        }

        if (string.IsNullOrWhiteSpace(request.ToFactoryId))
        {
            errors["toFactoryId"] = ["The target supplier is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ToPage))
        {
            errors["toPage"] = ["The target production page is required."];
        }

        if (request.Reason == TransferReason.Other && string.IsNullOrWhiteSpace(request.Note))
        {
            errors["note"] = ["A note is required when the reason is Other."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task<PlannedTransfer> PlanItem(
        Allocation allocation,
        TransferRequest request,
        string quoteMode,
        List<ShippingRoute> routes,
        BalancerSettings settings,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        TransferItemDto Item(
            string newQuoteId = null,
            decimal? costDelta = null,
            int? capacity = null,
            DateOnly? newEta = null,
            bool skipped = false,
            string problem = null)
        {
            return new TransferItemDto(
                allocation.Id,
                allocation.OrderId,
                allocation.OrderItemId,
                allocation.FactoryId,
                allocation.ProductionPage,
                request.ToFactoryId,
                request.ToPage,
                allocation.SelectedQuoteId,
                newQuoteId,
                costDelta,
                allocation.Currency,
                capacity,
                allocation.EtaTo,
                newEta,
                skipped,
                problem);
        }

        if (allocation.Status != AllocationStatus.Allocated || allocation.Mode == AllocationMode.Shadow || allocation.FactoryId == null)
        {
            return new PlannedTransfer(allocation, Item(problem: "The item is not open for transfer"), null, null, null);
        }

        if (allocation.FactoryId == request.ToFactoryId)
        {
            return allocation.ProductionPage == request.ToPage
                ? new PlannedTransfer(allocation, Item(skipped: true), null, null, null)
                : new PlannedTransfer(allocation, Item(allocation.SelectedQuoteId, 0m, null, allocation.EtaTo), null, null, null);
        }

        QuoteSet quoteSet;
        try
        {
            quoteSet = await catalogClient.RequestQuotes(
                new QuoteRequest(
                    Guid.NewGuid(),
                    allocation.OrderId,
                    allocation.OrderItemId,
                    allocation.SkuUuid,
                    allocation.Quantity,
                    allocation.CbdJson.FromJsonString<List<CbdComponent>>() ?? [],
                    quoteMode),
                cancellationToken);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Re-quote failed for order {OrderId} item {OrderItemId}", allocation.OrderId, allocation.OrderItemId);
            return new PlannedTransfer(allocation, Item(problem: "The production quote is unavailable"), null, null, null);
        }

        var target = quoteSet.Quotes.FirstOrDefault(q => q.FactoryId == request.ToFactoryId);
        if (target == null || !target.Producible || target.Cost == null)
        {
            var reasons = target?.Reasons is { Count: > 0 } ? string.Join(",", target.Reasons) : ExclusionReasons.NotProducible;
            return new PlannedTransfer(allocation, Item(target?.QuoteId, problem: reasons), quoteSet, target, null);
        }

        var route = routes.FirstOrDefault(r => r.DestinationCountry == allocation.DestinationCountry);
        if (route == null)
        {
            return new PlannedTransfer(allocation, Item(target.QuoteId, capacity: target.CapacitySignalPct, problem: ExclusionReasons.NoRoute), quoteSet, target, null);
        }

        var newTotal = target.Cost.Total + route.CostAmount;
        var oldTotal = allocation.TotalCost ?? allocation.ProductionCost;

        return new PlannedTransfer(
            allocation,
            Item(
                target.QuoteId,
                oldTotal.HasValue ? newTotal - oldTotal.Value : null,
                target.CapacitySignalPct,
                new RouteOption(allocation.FactoryId, route.OriginFacilityId, route.CostAmount, route.CostCurrency, route.TransitDaysMin, route.TransitDaysMax, route.WorkingDays)
                    .DeliveryAfter(today.AddDays(settings.DefaultProductionDays), route.TransitDaysMax)),
            quoteSet,
            target,
            route.CostAmount);
    }
}
