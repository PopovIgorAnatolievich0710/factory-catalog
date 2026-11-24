using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Skus.List;

public class SkuListReader(IFactoryCatalogDbContext db, ProductionConflictReader conflictReader)
{
    private static readonly Dictionary<string, Expression<Func<Sku, object>>> SortableFields = new()
    {
        ["siteSku"] = s => s.SiteSku,
        ["factorySku"] = s => s.Aliases.Min(a => a.Alias),
        ["name"] = s => s.Name,
        ["productType"] = s => s.ProductType,
        ["status"] = s => s.Status,
        ["validationStatus"] = s => s.ValidationStatus,
        ["skuUuid"] = s => s.SkuUuid,
        ["dropshipperName"] = s => s.DropshipperName,
        ["lastMasterSyncAt"] = s => s.LastMasterSyncAt
    };

    public async Task<ListResult<SkuListItemDto>> Read(ListRequest request, bool paged, CancellationToken cancellationToken)
    {
        var query = db.Skus.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.SiteSku.ToLower().Contains(search)
                                     || s.Name.ToLower().Contains(search)
                                     || s.SkuUuid.ToString().Contains(search)
                                     || s.Aliases.Any(a => a.Alias.ToLower().Contains(search)));
        }

        var aliasFilter = request.Filter("factorySku");
        var aliasPredicate = aliasFilter == null
            ? null
            : ListFilterExpression.Build<SkuAlias, string>("factorySku", aliasFilter, a => a.Alias);
        if (aliasPredicate != null)
        {
            query = query.Where(s => s.Aliases.AsQueryable().Any(aliasPredicate));
        }

        var producibleAt = request.Filter("producibleAt")?.Values?.ToList();
        if (producibleAt is { Count: > 0 })
        {
            query = query.Where(s => s.Factories.Any(f => f.Producible && producibleAt.Contains(f.FactoryId)));
        }

        query = query
            .ApplyFilter(request, "siteSku", s => s.SiteSku)
            .ApplyFilter(request, "name", s => s.Name)
            .ApplyFilter(request, "productType", s => s.ProductType)
            .ApplyFilter(request, "status", s => s.Status)
            .ApplyFilter(request, "validationStatus", s => s.ValidationStatus)
            .ApplyFilter(request, "dropshipperName", s => s.DropshipperName)
            .ApplySorting(request, SortableFields, "siteSku");

        var totalCount = await query.CountAsync(cancellationToken);
        var skus = await (paged ? query.ApplyPaging(request) : query)
            .Select(s => new
            {
                s.SkuUuid,
                s.SiteSku,
                Aliases = s.Aliases.OrderBy(a => a.Alias).Select(a => a.Alias).ToList(),
                s.Name,
                s.ProductType,
                s.Status,
                ProducibleAt = s.Factories.Where(f => f.Producible).OrderBy(f => f.FactoryId).Select(f => f.FactoryId).ToList(),
                s.ValidationStatus,
                s.DropshipperName,
                s.LastMasterSyncAt
            })
            .ToListAsync(cancellationToken);

        var conflicts = await conflictReader.Get(skus.Select(s => s.SkuUuid).ToList(), cancellationToken);
        var items = skus
            .Select(s => new SkuListItemDto(
                s.SkuUuid,
                s.SiteSku,
                s.Aliases,
                s.Name,
                s.ProductType,
                s.Status,
                s.ProducibleAt,
                s.ValidationStatus,
                conflicts.TryGetValue(s.SkuUuid, out var issues) ? issues.Select(i => i.FactoryId).Distinct().ToList() : [],
                s.DropshipperName,
                s.LastMasterSyncAt))
            .ToList();

        return new ListResult<SkuListItemDto>(items, totalCount);
    }
}
