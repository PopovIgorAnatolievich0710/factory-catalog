using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.LegacyBalancer;

internal static class SkipLogList
{
    private const string DefaultSortBy = "createdAt";

    private static readonly Dictionary<string, Expression<Func<BalancerSkipLog, object>>> SortableFields = new()
    {
        [DefaultSortBy] = l => l.CreatedAt,
        ["orderId"] = l => l.OrderId,
        ["orderItemId"] = l => l.OrderItemId,
        ["factoryId"] = l => l.FactoryId,
        ["reason"] = l => l.Reason,
        ["componentErpId"] = l => l.ComponentErpId,
        ["processId"] = l => l.ProcessId
    };

    public static IQueryable<SkipLogEntryDto> Query(IFactoryCatalogDbContext db, ListRequest request)
    {
        var query = db.BalancerSkipLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(l => l.OrderId.ToLower().Contains(search));
        }

        return query
            .ApplyFilter(request, "orderId", l => l.OrderId)
            .ApplyFilter(request, "orderItemId", l => l.OrderItemId)
            .ApplyFilter(request, "skuUuid", l => l.SkuUuid)
            .ApplyFilter(request, "factoryId", l => l.FactoryId)
            .ApplyFilter(request, "reason", l => l.Reason)
            .ApplyFilter(request, "componentErpId", l => l.ComponentErpId)
            .ApplyFilter(request, "processId", l => l.ProcessId)
            .ApplyFilter(request, "createdAt", l => l.CreatedAt)
            .ApplySorting(request, SortableFields, DefaultSortBy)
            .Select(l => new SkipLogEntryDto(
                l.Id,
                l.OrderId,
                l.OrderItemId,
                l.SkuUuid,
                l.FactoryId,
                l.Reason,
                l.ComponentErpId,
                l.ProcessId,
                l.CreatedAt));
    }
}
