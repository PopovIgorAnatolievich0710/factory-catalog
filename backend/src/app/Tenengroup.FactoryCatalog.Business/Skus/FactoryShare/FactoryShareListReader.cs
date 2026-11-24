using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class FactoryShareListReader(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
{
    private static readonly Dictionary<string, Expression<Func<Sku, object>>> SortableFields = new()
    {
        ["siteSku"] = s => s.SiteSku,
        ["name"] = s => s.Name,
        ["status"] = s => s.Status
    };

    public async Task<ListResult<FactoryShareRowDto>> Read(ListRequest request, bool paged, CancellationToken cancellationToken)
    {
        var query = db.Skus.AsNoTracking().Where(s => s.ProductType == SkuProductType.Manufactured);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.SiteSku.ToLower().Contains(search)
                                     || s.Name.ToLower().Contains(search)
                                     || s.SkuUuid.ToString().Contains(search)
                                     || s.Aliases.Any(a => a.Alias.ToLower().Contains(search)));
        }

        var producibleAt = request.Filter("producibleAt")?.Values?.ToList();
        if (producibleAt is { Count: > 0 })
        {
            query = query.Where(s => s.Factories.Any(f => f.Producible && producibleAt.Contains(f.FactoryId)));
        }

        query = query
            .ApplyFilter(request, "siteSku", s => s.SiteSku)
            .ApplyFilter(request, "name", s => s.Name)
            .ApplyFilter(request, "status", s => s.Status)
            .ApplySorting(request, SortableFields, "siteSku");

        var totalCount = await query.CountAsync(cancellationToken);
        var skus = await (paged ? query.ApplyPaging(request) : query)
            .Select(s => new { s.SkuUuid, s.SiteSku, s.Name, s.Status, Factories = s.Factories.ToList() })
            .ToListAsync(cancellationToken);

        var factoryIds = await shareService.GetActiveFactoryIds(cancellationToken);
        var defaults = await shareService.GetDefaultShares(cancellationToken);
        var rows = skus
            .Select(s =>
            {
                var factories = SkuFactoryShareBuilder.Build(factoryIds, s.Factories, defaults);
                return new FactoryShareRowDto(s.SkuUuid, s.SiteSku, s.Name, s.Status, factories, factories.Sum(f => f.EffectiveSharePct));
            })
            .ToList();

        return new ListResult<FactoryShareRowDto>(rows, totalCount);
    }
}
