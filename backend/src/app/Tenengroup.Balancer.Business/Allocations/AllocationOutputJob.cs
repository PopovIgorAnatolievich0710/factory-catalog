using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Events;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Aws.Sns;

namespace Tenengroup.Balancer.Business.Allocations;

public class AllocationOutputJob(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    IEventPublisher eventPublisher,
    BalancerOptions options,
    TimeProvider timeProvider,
    ILogger<AllocationOutputJob> logger) : IAllocationOutputJob
{
    public async Task Publish(Guid allocationId, CancellationToken cancellationToken)
    {
        var allocation = await db.Allocations.FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);

        if (allocation == null
            || allocation.OutputPublishedAt != null
            || allocation.Mode == AllocationMode.Shadow
            || allocation.FactoryId == null)
        {
            return;
        }

        if (allocation.SelectedQuoteId != null)
        {
            await catalogClient.SelectQuote(allocation.SelectedQuoteId, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(options.EventsTopicArn))
        {
            logger.LogWarning("Balancer:EventsTopicArn is empty; allocation event for {AllocationId} is not published", allocationId);
        }
        else
        {
            await eventPublisher.Publish(options.EventsTopicArn, BalancerEventTypes.AllocationDecided, ToEvent(allocation), cancellationToken);
        }

        allocation.OutputPublishedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static AllocationDecidedEvent ToEvent(Allocation allocation)
    {
        var currency = allocation.Currency;

        return new AllocationDecidedEvent(
            allocation.OrderId,
            allocation.OrderItemId,
            allocation.QuoteKey,
            allocation.SelectedQuoteId,
            allocation.FactoryId,
            allocation.PackingFacilityId,
            ToMoney(allocation.ProductionCost, currency),
            ToMoney(allocation.ShippingCost, currency),
            ToMoney(allocation.TotalCost, currency),
            allocation.EtaFrom.HasValue && allocation.EtaTo.HasValue
                ? new EtaRange(allocation.EtaFrom.Value, allocation.EtaTo.Value)
                : null,
            allocation.Mode.ToString().ToLowerInvariant(),
            allocation.ReadCandidates()
                .Select(c => new AllocationDecidedCandidate(c.FactoryId, c.TotalCost, c.EtaTo, c.ExcludedReason))
                .ToList());
    }

    private static Money ToMoney(decimal? amount, string currency)
    {
        return amount.HasValue && currency != null ? new Money(amount.Value, currency) : null;
    }
}
