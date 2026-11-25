using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Decisions;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Allocations;

public class AllocateOrderCommandHandler(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    IDecisionContextFactory contextFactory,
    IRoutingDecisionEngine decisionEngine,
    IBalancerSettingsProvider settingsProvider,
    IAllocationOutputQueue outputQueue,
    TimeProvider timeProvider,
    ILogger<AllocateOrderCommandHandler> logger)
    : IRequestHandler<AllocateOrderCommand, IReadOnlyCollection<AllocationDto>>
{
    public async Task<IReadOnlyCollection<AllocationDto>> Handle(AllocateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ValidationException("items", "At least one order item is required.");
        }

        var settings = await settingsProvider.GetCurrent(cancellationToken);
        var isShadow = settings.Mode == BalancerMode.Shadow;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var existing = (await db.Allocations
                .Where(a => a.OrderId == request.OrderId)
                .ToListAsync(cancellationToken))
            .Where(a => (a.Mode == AllocationMode.Shadow) == isShadow)
            .ToList();

        var newItems = request.Items
            .Where(i => existing.All(a => a.OrderItemId != i.OrderItemId))
            .GroupBy(i => i.OrderItemId)
            .Select(g => g.First())
            .ToList();

        var created = new List<Allocation>();
        var quoted = new List<(AllocationItemRequest Item, QuoteSet QuoteSet)>();

        foreach (var item in newItems)
        {
            if (item.FulfillmentType != FulfillmentType.Factory)
            {
                created.Add(await NotRouted(request, item, item.FulfillmentType, isShadow, settings.Version, now, cancellationToken));
                continue;
            }

            var quoteSet = await RequestQuotes(request, item, isShadow, cancellationToken);
            if (IsDropshipper(quoteSet))
            {
                created.Add(await NotRouted(request, item, FulfillmentType.Dropshipper, isShadow, settings.Version, now, cancellationToken));
                continue;
            }

            quoted.Add((item, quoteSet));
        }

        if (quoted.Count > 0)
        {
            var anchors = existing
                .Where(a => a.FactoryId != null)
                .Select(a => a.FactoryId)
                .Distinct()
                .ToList();

            var input = await contextFactory.Create(
                new DecisionContextRequest(
                    request.DestinationCountry,
                    request.PromisedDeliveryDate,
                    request.SlaDeliveryDate,
                    request.IsEventOrder,
                    quoted.Select(q => new DecisionItem(q.Item.OrderItemId, q.QuoteSet)).ToList(),
                    anchors),
                settings,
                cancellationToken);

            var decision = decisionEngine.Decide(input);

            created.AddRange(quoted.Select(q => Routed(
                request,
                q.Item,
                q.QuoteSet,
                decision.Items.First(d => d.OrderItemId == q.Item.OrderItemId),
                input,
                isShadow,
                settings.Version,
                now)));
        }

        foreach (var allocation in created)
        {
            db.Allocations.Add(allocation);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var allocation in created.Where(a => a.FactoryId != null))
        {
            if (allocation.FallbackReason != null)
            {
                logger.LogError(
                    "BALANCER FALLBACK: order {OrderId} item {OrderItemId} routed to {FactoryId} by default share ({FallbackReason})",
                    allocation.OrderId, allocation.OrderItemId, allocation.FactoryId, allocation.FallbackReason);
            }

            if (allocation.IsSplit)
            {
                logger.LogWarning("Order {OrderId} is split: item {OrderItemId} goes to {FactoryId}",
                    allocation.OrderId, allocation.OrderItemId, allocation.FactoryId);
            }

            if (!isShadow)
            {
                outputQueue.Enqueue(allocation.Id);
            }
        }

        return existing
            .Where(a => request.Items.Any(i => i.OrderItemId == a.OrderItemId))
            .Concat(created)
            .Select(a => a.ToDto(null))
            .ToList();
    }

    private async Task<QuoteSet> RequestQuotes(
        AllocateOrderCommand request,
        AllocationItemRequest item,
        bool isShadow,
        CancellationToken cancellationToken)
    {
        try
        {
            return await catalogClient.RequestQuotes(
                new QuoteRequest(
                    Guid.NewGuid(),
                    request.OrderId,
                    item.OrderItemId,
                    item.SkuUuid,
                    item.Quantity,
                    item.Cbd ?? [],
                    isShadow ? QuoteModes.Shadow : QuoteModes.Live),
                cancellationToken);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Production quotes are unavailable for order {OrderId} item {OrderItemId}", request.OrderId, item.OrderItemId);
            return null;
        }
    }

    private static bool IsDropshipper(QuoteSet quoteSet)
    {
        return quoteSet is { Quotes.Count: > 0 }
               && quoteSet.Quotes.All(q => q.Reasons != null && q.Reasons.Contains(QuoteReasonCodes.Dropshipper));
    }

    private async Task<Allocation> NotRouted(
        AllocateOrderCommand request,
        AllocationItemRequest item,
        FulfillmentType fulfillmentType,
        bool isShadow,
        int settingsVersion,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var allocation = NewAllocation(request, item, isShadow, settingsVersion, now);
        allocation.FulfillmentType = fulfillmentType;
        allocation.Status = AllocationStatus.NotRouted;
        allocation.Mode = isShadow ? AllocationMode.Shadow : AllocationMode.Live;
        allocation.DecisionReason = $"{fulfillmentType} item: not routed to a TG factory";

        if (fulfillmentType == FulfillmentType.Dropshipper)
        {
            try
            {
                var cost = await catalogClient.GetDropshipperCost(item.SkuUuid, cancellationToken);
                allocation.TotalCost = cost.TotalCost?.Amount;
                allocation.Currency = cost.TotalCost?.Currency;
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Dropshipper cost is unavailable for order {OrderId} item {OrderItemId}", request.OrderId, item.OrderItemId);
            }
        }

        return allocation;
    }

    private static Allocation Routed(
        AllocateOrderCommand request,
        AllocationItemRequest item,
        QuoteSet quoteSet,
        ItemDecision decision,
        DecisionInput input,
        bool isShadow,
        int settingsVersion,
        DateTime now)
    {
        var winner = decision.Winner;
        var allocation = NewAllocation(request, item, isShadow, settingsVersion, now);

        allocation.FulfillmentType = FulfillmentType.Factory;
        allocation.Status = AllocationStatus.Allocated;
        allocation.Mode = isShadow ? AllocationMode.Shadow : decision.IsFallback ? AllocationMode.Fallback : AllocationMode.Live;
        allocation.FallbackReason = decision.FallbackReason;
        allocation.FactoryId = winner.FactoryId;
        allocation.OriginalFactoryId = winner.FactoryId;
        allocation.PackingFacilityId = input.PackingFacilityIds.GetValueOrDefault(winner.FactoryId);
        allocation.ShippingCost = winner.ShippingCost;
        allocation.TotalCost = winner.TotalCost;
        allocation.CheapestFeasibleTotalCost = decision.CheapestFeasibleTotalCost;
        allocation.EtaFrom = winner.EtaFrom;
        allocation.EtaTo = winner.EtaTo;
        allocation.MeetsPromise = winner.MeetsPromise;
        allocation.IsSplit = decision.IsSplit;
        allocation.DecisionReason = decision.Reason;
        allocation.CandidatesJson = decision.Candidates.ToJsonString();
        allocation.DueDate = DueDate(winner, input);

        var quote = quoteSet?.Quotes.FirstOrDefault(q => q.FactoryId == winner.FactoryId);
        if (quote is { Producible: true, Cost: not null })
        {
            allocation.ApplyQuote(quoteSet.QuoteKey, quote);
            allocation.CostLines = quote.ToCostLines();
        }

        return allocation;
    }

    internal static DateOnly DueDate(Candidate winner, DecisionInput input)
    {
        if (winner.EtaTo == null || winner.TransitDaysMax == null)
        {
            return input.Today.AddDays(input.Settings.DefaultProductionDays);
        }

        var deliveryDate = input.PromisedDeliveryDate ?? winner.EtaTo.Value;
        var route = input.Routes.FirstOrDefault(r => r.FactoryId == winner.FactoryId);
        var shipBy = route?.LatestShipDate(deliveryDate, winner.TransitDaysMax.Value)
                     ?? deliveryDate.AddDays(-winner.TransitDaysMax.Value);
        return shipBy < input.Today ? input.Today : shipBy;
    }

    private static Allocation NewAllocation(
        AllocateOrderCommand request,
        AllocationItemRequest item,
        bool isShadow,
        int settingsVersion,
        DateTime now)
    {
        return new Allocation
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            OrderItemId = item.OrderItemId,
            SkuUuid = item.SkuUuid,
            Quantity = item.Quantity,
            DestinationCountry = request.DestinationCountry.ToUpperInvariant(),
            PromisedDeliveryDate = request.PromisedDeliveryDate,
            SlaDeliveryDate = request.SlaDeliveryDate,
            IsEventOrder = request.IsEventOrder,
            EventName = request.EventName,
            Site = request.Site,
            PaidAt = request.PaidAt,
            CbdJson = (item.Cbd ?? []).ToJsonString(),
            Mode = isShadow ? AllocationMode.Shadow : AllocationMode.Live,
            SettingsVersion = settingsVersion,
            DecidedAt = now
        };
    }
}
