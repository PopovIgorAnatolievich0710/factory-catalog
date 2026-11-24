using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Events;
using Tenengroup.Balancer.Business.Abstractions.Transfers;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Aws.Sns;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Transfers;

internal static class OpenItems
{
    private static readonly Dictionary<string, Expression<Func<Allocation, object>>> SortableFields = new()
    {
        ["dueDate"] = a => a.DueDate,
        ["orderId"] = a => a.OrderId,
        ["factoryId"] = a => a.FactoryId,
        ["productionPage"] = a => a.ProductionPage,
        ["eventName"] = a => a.EventName,
        ["site"] = a => a.Site
    };

    public static IQueryable<Allocation> Query(IBalancerDbContext db, ListRequest request)
    {
        var allocations = db.Allocations
            .AsNoTracking()
            .Where(a => a.Status == AllocationStatus.Allocated && a.Mode != AllocationMode.Shadow && a.FactoryId != null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            allocations = allocations.Where(a => a.OrderId.ToLower().Contains(search));
        }

        return allocations
            .ApplyFilter(request, "orderId", a => a.OrderId)
            .ApplyFilter(request, "factoryId", a => a.FactoryId)
            .ApplyFilter(request, "productionPage", a => a.ProductionPage)
            .ApplyFilter(request, "eventName", a => a.EventName)
            .ApplyFilter(request, "isEventOrder", a => a.IsEventOrder)
            .ApplyFilter(request, "site", a => a.Site)
            .ApplyFilter(request, "dueDate", a => a.DueDate)
            .ApplySorting(request, SortableFields, "dueDate");
    }
}

public class SearchOpenItemsQueryHandler(IBalancerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SearchOpenItemsQuery, ListResult<OpenItemDto>>
{
    public async Task<ListResult<OpenItemDto>> Handle(SearchOpenItemsQuery query, CancellationToken cancellationToken)
    {
        var page = await OpenItems.Query(db, query.Request).ToListResult(query.Request, cancellationToken);
        var anyFactory = await currentUser.CanAccessAnyFactory(cancellationToken);
        var ownFactories = anyFactory ? [] : await currentUser.GetFactoryIds(cancellationToken);

        return new ListResult<OpenItemDto>(
            page.Items
                .Select(a => new OpenItemDto(
                    a.Id,
                    a.OrderId,
                    a.OrderItemId,
                    a.SkuUuid,
                    a.FactoryId,
                    a.ProductionPage,
                    a.EventName,
                    a.Site,
                    a.DueDate,
                    a.SelectedQuoteId,
                    anyFactory || ownFactories.Contains(a.FactoryId)))
                .ToList(),
            page.TotalCount);
    }
}

public class GetOpenItemIdsQueryHandler(IBalancerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetOpenItemIdsQuery, IReadOnlyCollection<Guid>>
{
    public async Task<IReadOnlyCollection<Guid>> Handle(GetOpenItemIdsQuery query, CancellationToken cancellationToken)
    {
        var items = await OpenItems.Query(db, query.Request)
            .Select(a => new { a.Id, a.FactoryId })
            .ToListAsync(cancellationToken);

        if (await currentUser.CanAccessAnyFactory(cancellationToken))
        {
            return items.Select(a => a.Id).ToList();
        }

        var ownFactories = await currentUser.GetFactoryIds(cancellationToken);
        return items.Where(a => ownFactories.Contains(a.FactoryId)).Select(a => a.Id).ToList();
    }
}

public class GetProductionPagesQueryHandler(IBalancerDbContext db, BalancerOptions options)
    : IRequestHandler<GetProductionPagesQuery, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> Handle(GetProductionPagesQuery request, CancellationToken cancellationToken)
    {
        var used = await db.Allocations
            .AsNoTracking()
            .Where(a => a.FactoryId == request.FactoryId && a.ProductionPage != null)
            .Select(a => a.ProductionPage)
            .Distinct()
            .ToListAsync(cancellationToken);

        return options.ProductionPages.GetValueOrDefault(request.FactoryId, [])
            .Concat(used)
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }
}

public class PreviewTransferQueryHandler(ITransferPlanner planner)
    : IRequestHandler<PreviewTransferQuery, IReadOnlyCollection<TransferItemDto>>
{
    public async Task<IReadOnlyCollection<TransferItemDto>> Handle(PreviewTransferQuery request, CancellationToken cancellationToken)
    {
        var planned = await planner.Plan(request.Request, QuoteModes.Estimate, cancellationToken);
        return planned.Select(p => p.Item).ToList();
    }
}

public class TransferItemsCommandHandler(
    IBalancerDbContext db,
    ITransferPlanner planner,
    ICatalogClient catalogClient,
    IEventPublisher eventPublisher,
    ICurrentUser currentUser,
    BalancerEventsOptions options,
    TimeProvider timeProvider,
    ILogger<TransferItemsCommandHandler> logger) : IRequestHandler<TransferItemsCommand, TransferResultDto>
{
    public async Task<TransferResultDto> Handle(TransferItemsCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var planned = await planner.Plan(request, QuoteModes.Live, cancellationToken);
        var results = new List<TransferItemDto>();
        var transferred = 0;

        foreach (var plan in planned)
        {
            if (!plan.IsExecutable)
            {
                results.Add(plan.Item);
                continue;
            }

            try
            {
                await Execute(plan, request, cancellationToken);
                results.Add(plan.Item);
                transferred++;
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Transfer of order {OrderId} item {OrderItemId} failed", plan.Item.OrderId, plan.Item.OrderItemId);
                results.Add(plan.Item with { Problem = "The factory catalog rejected the quote change" });
            }
        }

        return new TransferResultDto(transferred, results);
    }

    private async Task Execute(PlannedTransfer plan, TransferRequest request, CancellationToken cancellationToken)
    {
        var allocation = plan.Allocation;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var transfer = new AllocationTransfer
        {
            AllocationId = allocation.Id,
            OrderId = allocation.OrderId,
            OrderItemId = allocation.OrderItemId,
            SkuUuid = allocation.SkuUuid,
            FromFactoryId = allocation.FactoryId,
            FromPage = allocation.ProductionPage,
            ToFactoryId = request.ToFactoryId,
            ToPage = request.ToPage,
            Reason = request.Reason,
            Note = request.Note,
            OldQuoteId = allocation.SelectedQuoteId,
            NewQuoteId = plan.Item.NewQuoteId,
            CostDelta = plan.Item.CostDelta,
            Currency = plan.TargetQuote?.Cost?.Currency ?? allocation.Currency,
            OldEta = allocation.EtaTo,
            NewEta = plan.Item.NewEta,
            TransferredBy = currentUser.Actor,
            Initiator = TransferInitiator.User,
            TransferredAt = now
        };

        if (plan.TargetQuote != null)
        {
            if (allocation.SelectedQuoteId != null)
            {
                await catalogClient.MarkQuoteTransferred(allocation.SelectedQuoteId, cancellationToken);
            }

            await catalogClient.SelectQuote(plan.TargetQuote.QuoteId, cancellationToken);

            var oldLines = await db.AllocationCostLines
                .Where(l => l.AllocationId == allocation.Id)
                .ToListAsync(cancellationToken);
            db.AllocationCostLines.RemoveRange(oldLines);

            foreach (var line in plan.TargetQuote.ToCostLines())
            {
                line.AllocationId = allocation.Id;
                db.AllocationCostLines.Add(line);
            }

            allocation.ApplyQuote(plan.QuoteSet.QuoteKey, plan.TargetQuote);
            allocation.FactoryId = request.ToFactoryId;
            allocation.PackingFacilityId = request.ToFactoryId;
            allocation.ShippingCost = plan.ShippingCost;
            allocation.TotalCost = plan.TargetQuote.Cost.Total + (plan.ShippingCost ?? 0m);
            allocation.EtaFrom = null;
            allocation.EtaTo = plan.Item.NewEta;
            allocation.TransferCount++;
        }

        allocation.ProductionPage = request.ToPage;
        db.AllocationTransfers.Add(transfer);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.AllocationDecidedTopicArn))
        {
            await eventPublisher.Publish(
                options.AllocationDecidedTopicArn,
                BalancerEventTypes.AllocationTransferred,
                new AllocationTransferredEvent(
                    transfer.OrderId,
                    transfer.OrderItemId,
                    transfer.FromFactoryId,
                    transfer.FromPage,
                    transfer.ToFactoryId,
                    transfer.ToPage,
                    transfer.OldQuoteId,
                    transfer.NewQuoteId,
                    transfer.Reason.ToString(),
                    transfer.Note,
                    transfer.TransferredBy,
                    transfer.TransferredAt),
                cancellationToken);
        }
    }
}

public class SearchTransfersQueryHandler(IBalancerDbContext db)
    : IRequestHandler<SearchTransfersQuery, ListResult<TransferLogDto>>
{
    private static readonly Dictionary<string, Expression<Func<AllocationTransfer, object>>> SortableFields = new()
    {
        ["transferredAt"] = t => t.TransferredAt,
        ["orderId"] = t => t.OrderId,
        ["fromFactoryId"] = t => t.FromFactoryId,
        ["toFactoryId"] = t => t.ToFactoryId,
        ["reason"] = t => t.Reason,
        ["transferredBy"] = t => t.TransferredBy
    };

    public async Task<ListResult<TransferLogDto>> Handle(SearchTransfersQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        var transfers = db.AllocationTransfers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            transfers = transfers.Where(t =>
                t.OrderId.ToLower().Contains(search)
                || t.SkuUuid.ToLower().Contains(search)
                || t.TransferredBy.ToLower().Contains(search)
                || t.ToPage.ToLower().Contains(search)
                || (t.FromPage != null && t.FromPage.ToLower().Contains(search))
                || (t.Note != null && t.Note.ToLower().Contains(search)));
        }

        var supplier = request.Filter("supplier");
        if (supplier?.Values is { Length: > 0 })
        {
            transfers = transfers.Where(t => supplier.Values.Contains(t.FromFactoryId) || supplier.Values.Contains(t.ToFactoryId));
        }

        return await transfers
            .ApplyFilter(request, "orderId", t => t.OrderId)
            .ApplyFilter(request, "fromFactoryId", t => t.FromFactoryId)
            .ApplyFilter(request, "toFactoryId", t => t.ToFactoryId)
            .ApplyFilter(request, "fromPage", t => t.FromPage)
            .ApplyFilter(request, "toPage", t => t.ToPage)
            .ApplyFilter(request, "reason", t => t.Reason)
            .ApplyFilter(request, "transferredBy", t => t.TransferredBy)
            .ApplyFilter(request, "transferredAt", t => t.TransferredAt)
            .ApplySorting(request, SortableFields, "transferredAt")
            .Select(t => new TransferLogDto(
                t.Id,
                t.TransferredAt,
                t.TransferredBy,
                t.Initiator,
                t.OrderId,
                t.OrderItemId,
                t.SkuUuid,
                t.FromFactoryId,
                t.FromPage,
                t.ToFactoryId,
                t.ToPage,
                t.Reason,
                t.Note,
                t.OldQuoteId,
                t.NewQuoteId,
                t.CostDelta,
                t.Currency,
                t.OldEta,
                t.NewEta))
            .ToListResult(request, cancellationToken);
    }
}
