using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class GetGlobalFactoryShareQueryHandler(IFactoryCatalogDbContext db, SkuFactoryShareService shareService)
    : IRequestHandler<GetGlobalFactoryShareQuery, GlobalFactoryShareDto>
{
    public async Task<GlobalFactoryShareDto> Handle(GetGlobalFactoryShareQuery request, CancellationToken cancellationToken)
    {
        var factoryIds = await shareService.GetActiveFactoryIds(cancellationToken);
        var rows = await db.GlobalFactoryShares
            .AsNoTracking()
            .Where(s => factoryIds.Contains(s.FactoryId))
            .ToListAsync(cancellationToken);
        var shares = factoryIds
            .Select(id => new FactorySharePctDto(id, rows.FirstOrDefault(r => r.FactoryId == id)?.Pct ?? 0))
            .ToList();
        var lastUpdated = rows.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();

        return new GlobalFactoryShareDto(shares, shares.Sum(s => s.Pct), lastUpdated?.UpdatedBy, lastUpdated?.UpdatedAt);
    }
}
