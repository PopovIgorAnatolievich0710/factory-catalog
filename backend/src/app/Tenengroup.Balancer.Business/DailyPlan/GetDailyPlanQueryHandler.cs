using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.DailyPlan;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.DailyPlan;

public class GetDailyPlanQueryHandler(
    IBalancerDbContext db,
    ICatalogClient catalogClient,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<GetDailyPlanQueryHandler> logger) : IRequestHandler<GetDailyPlanQuery, DailyPlanDto>
{
    public async Task<DailyPlanDto> Handle(GetDailyPlanQuery request, CancellationToken cancellationToken)
    {
        await currentUser.EnsureCanAccessFactory(request.FactoryId, cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var includeOverdue = request.Date == today;

        var allocations = await db.Allocations
            .AsNoTracking()
            .Where(a => a.FactoryId == request.FactoryId
                        && a.Status == AllocationStatus.Allocated
                        && a.Mode != AllocationMode.Shadow
                        && (a.DueDate == request.Date || (includeOverdue && (a.DueDate == null || a.DueDate < request.Date))))
            .ToListAsync(cancellationToken);

        var allocationIds = allocations.Select(a => a.Id).ToList();
        var lines = await db.AllocationCostLines
            .AsNoTracking()
            .Where(l => allocationIds.Contains(l.AllocationId) && l.Kind != CostLineKind.Component)
            .ToListAsync(cancellationToken);

        var capacity = await GetCapacity(request, cancellationToken);

        var processes = lines
            .GroupBy(l => l.Code)
            .Select(group =>
            {
                var process = capacity?.FirstOrDefault(c => c.ProcessId == group.Key);
                var items = group.Select(l => l.AllocationId).Distinct().Count();
                var available = process?.MaxPerDay * process?.CapacityPct / 100m;
                var done = process?.UsedToday;
                var planned = (done ?? 0m) + items;

                return new DailyPlanProcessDto(
                    group.Key,
                    process?.Name ?? group.Key,
                    process?.Type,
                    process?.MeasuredIn,
                    items,
                    group.Sum(l => l.Quantity),
                    done,
                    available,
                    available > 0 ? Math.Round(planned / available.Value * 100m, 1) : null,
                    available.HasValue && planned > available.Value,
                    group
                        .Join(allocations, l => l.AllocationId, a => a.Id, (l, a) => new DailyPlanItemDto(
                            a.Id, a.OrderId, a.OrderItemId, a.SkuUuid, a.SelectedQuoteId, a.DueDate, l.Quantity))
                        .OrderBy(i => i.DueDate)
                        .ThenBy(i => i.OrderId)
                        .ToList());
            })
            .OrderByDescending(p => p.IsOverCapacity)
            .ThenByDescending(p => p.LoadPct ?? 0m)
            .ThenBy(p => p.Name)
            .ToList();

        return new DailyPlanDto(request.FactoryId, request.Date, allocations.Count, capacity != null, processes);
    }

    private async Task<IReadOnlyCollection<ProcessCapacity>> GetCapacity(GetDailyPlanQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await catalogClient.GetProcessCapacity(request.FactoryId, request.Date, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Process capacity of factory {FactoryId} is unavailable; the daily plan is shown without capacity", request.FactoryId);
            return null;
        }
    }
}
