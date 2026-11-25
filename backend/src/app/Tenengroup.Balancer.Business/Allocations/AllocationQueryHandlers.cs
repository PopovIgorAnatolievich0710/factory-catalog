using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Decisions;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Allocations;

public class SearchAllocationsQueryHandler(IBalancerDbContext db)
    : IRequestHandler<SearchAllocationsQuery, ListResult<AllocationDto>>
{
    private static readonly Dictionary<string, Expression<Func<Allocation, object>>> SortableFields = new()
    {
        ["decidedAt"] = a => a.DecidedAt,
        ["orderId"] = a => a.OrderId,
        ["factoryId"] = a => a.FactoryId,
        ["totalCost"] = a => a.TotalCost,
        ["mode"] = a => a.Mode
    };

    public async Task<ListResult<AllocationDto>> Handle(SearchAllocationsQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        var allocations = db.Allocations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            allocations = allocations.Where(a => a.OrderId.ToLower().Contains(search));
        }

        var page = await allocations
            .ApplyFilter(request, "mode", a => a.Mode)
            .ApplyFilter(request, "status", a => a.Status)
            .ApplyFilter(request, "factoryId", a => a.FactoryId)
            .ApplyFilter(request, "destinationCountry", a => a.DestinationCountry)
            .ApplyFilter(request, "fulfillmentType", a => a.FulfillmentType)
            .ApplyFilter(request, "orderId", a => a.OrderId)
            .ApplyFilter(request, "decidedAt", a => a.DecidedAt)
            .ApplySorting(request, SortableFields, "decidedAt")
            .ToListResult(request, cancellationToken);

        var orderIds = page.Items.Select(a => a.OrderId).Distinct().ToList();
        var legacy = await db.LegacyDecisions
            .AsNoTracking()
            .Where(d => orderIds.Contains(d.OrderId))
            .ToListAsync(cancellationToken);

        return new ListResult<AllocationDto>(
            page.Items
                .Select(a => a.ToDto(legacy.FirstOrDefault(d => d.OrderId == a.OrderId && d.OrderItemId == a.OrderItemId)?.FactoryId))
                .ToList(),
            page.TotalCount);
    }
}

public class GetAllocationQueryHandler(IBalancerDbContext db) : IRequestHandler<GetAllocationQuery, AllocationDto>
{
    public async Task<AllocationDto> Handle(GetAllocationQuery request, CancellationToken cancellationToken)
    {
        var allocation = await db.Allocations.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
                         ?? throw new EntityNotFoundException("Allocation", request.Id);

        var legacy = await db.LegacyDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.OrderId == allocation.OrderId && d.OrderItemId == allocation.OrderItemId, cancellationToken);

        return allocation.ToDto(legacy?.FactoryId);
    }
}

public class EstimateAllocationQueryHandler(
    ICatalogClient catalogClient,
    IDecisionContextFactory contextFactory,
    IRoutingDecisionEngine decisionEngine,
    IBalancerSettingsProvider settingsProvider) : IRequestHandler<EstimateAllocationQuery, EstimateDto>
{
    private const string EstimateItemId = "estimate";

    public async Task<EstimateDto> Handle(EstimateAllocationQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.GetCurrent(cancellationToken);

        QuoteSet quoteSet;
        try
        {
            quoteSet = await catalogClient.RequestQuotes(
                new QuoteRequest(Guid.NewGuid(), null, null, request.SkuUuid, request.Quantity, request.Cbd ?? [], QuoteModes.Estimate),
                cancellationToken);
        }
        catch (HttpRequestException e) when ((int?)e.StatusCode is null or >= 500)
        {
            quoteSet = null;
        }

        var input = await contextFactory.Create(
            new DecisionContextRequest(
                request.DestinationCountry,
                request.PromisedDeliveryDate,
                null,
                false,
                [new DecisionItem(EstimateItemId, quoteSet)],
                []),
            settings,
            cancellationToken);

        var decision = decisionEngine.Decide(input).Items.First();

        return new EstimateDto(
            decision.Winner.FactoryId,
            input.PackingFacilityIds.GetValueOrDefault(decision.Winner.FactoryId),
            decision.Winner.EtaFrom,
            decision.Winner.EtaTo,
            decision.Winner.TotalCost,
            decision.Winner.Currency,
            decision.IsFallback,
            decision.Candidates);
    }
}

public class RecordLegacyDecisionCommandHandler(IBalancerDbContext db, TimeProvider timeProvider)
    : IRequestHandler<RecordLegacyDecisionCommand>
{
    public async Task Handle(RecordLegacyDecisionCommand request, CancellationToken cancellationToken)
    {
        var decision = await db.LegacyDecisions.FirstOrDefaultAsync(
            d => d.OrderId == request.OrderId && d.OrderItemId == request.OrderItemId, cancellationToken);

        if (decision == null)
        {
            decision = new LegacyDecision { OrderId = request.OrderId, OrderItemId = request.OrderItemId };
            db.LegacyDecisions.Add(decision);
        }

        decision.FactoryId = request.FactoryId;
        decision.TotalCost = request.TotalCost;
        decision.Eta = request.Eta;
        decision.ReceivedAt = timeProvider.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync(cancellationToken);
    }
}
