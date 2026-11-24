using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Quotes;

internal static class SkuLookup
{
    public static async Task<Sku> Find(IFactoryCatalogDbContext db, string identifier, CancellationToken cancellationToken)
    {
        var value = identifier.Trim();

        if (Guid.TryParse(value, out var skuUuid))
        {
            var byUuid = await db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuUuid == skuUuid, cancellationToken);
            if (byUuid != null)
            {
                return byUuid;
            }
        }

        var lowered = value.ToLower();
        var alias = await db.SkuAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Alias.ToLower() == lowered, cancellationToken);

        if (alias != null)
        {
            return await db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuUuid == alias.SkuUuid, cancellationToken);
        }

        var bySiteSku = await db.Skus
            .AsNoTracking()
            .Where(s => s.SiteSku.ToLower() == lowered)
            .Take(2)
            .ToListAsync(cancellationToken);

        return bySiteSku.Count switch
        {
            0 => null,
            1 => bySiteSku[0],
            _ => throw new ValidationException("sku", $"Site SKU '{value}' matches more than one SKU; use the SKU UUID.")
        };
    }
}
