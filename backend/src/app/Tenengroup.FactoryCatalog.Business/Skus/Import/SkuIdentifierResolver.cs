using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.Import;

public record ResolvedSku(Guid SkuUuid, SkuProductType ProductType);

public class SkuIdentifierResolution(
    IReadOnlyDictionary<Guid, ResolvedSku> byUuid,
    IReadOnlyDictionary<string, ResolvedSku> byAlias,
    ILookup<string, ResolvedSku> bySiteSku)
{
    public ResolvedSku Resolve(string identifier, out string error)
    {
        error = null;
        var value = identifier.Trim();
        if (Guid.TryParse(value, out var uuid) && byUuid.TryGetValue(uuid, out var sku))
        {
            return sku;
        }

        var lowered = value.ToLower();
        if (byAlias.TryGetValue(lowered, out var aliased))
        {
            return aliased;
        }

        var bySite = bySiteSku[lowered].ToList();
        if (bySite.Count == 1)
        {
            return bySite[0];
        }

        error = bySite.Count > 1
            ? $"Site SKU '{value}' matches more than one SKU; use the SKU UUID."
            : $"SKU '{value}' is not found by UUID, alias or Site SKU.";
        return null;
    }
}

public class SkuIdentifierResolver(IFactoryCatalogDbContext db)
{
    public async Task<SkuIdentifierResolution> Load(IReadOnlyCollection<string> identifiers, CancellationToken cancellationToken)
    {
        var values = identifiers.Select(i => i.Trim()).Where(i => i.Length > 0).Distinct().ToList();
        var lowered = values.Select(i => i.ToLower()).Distinct().ToList();
        var uuids = values
            .Select(i => Guid.TryParse(i, out var uuid) ? uuid : (Guid?)null)
            .OfType<Guid>()
            .ToList();

        var skus = await db.Skus
            .AsNoTracking()
            .Where(s => uuids.Contains(s.SkuUuid) || lowered.Contains(s.SiteSku.ToLower()))
            .Select(s => new { s.SkuUuid, s.SiteSku, s.ProductType })
            .ToListAsync(cancellationToken);
        var aliases = await db.SkuAliases
            .AsNoTracking()
            .Where(a => lowered.Contains(a.Alias.ToLower()))
            .Join(db.Skus.AsNoTracking(), a => a.SkuUuid, s => s.SkuUuid, (a, s) => new { a.Alias, a.SkuUuid, s.ProductType })
            .ToListAsync(cancellationToken);

        return new SkuIdentifierResolution(
            skus.ToDictionary(s => s.SkuUuid, s => new ResolvedSku(s.SkuUuid, s.ProductType)),
            aliases
                .GroupBy(a => a.Alias.ToLower())
                .ToDictionary(g => g.Key, g => new ResolvedSku(g.First().SkuUuid, g.First().ProductType)),
            skus.ToLookup(s => s.SiteSku.ToLower(), s => new ResolvedSku(s.SkuUuid, s.ProductType)));
    }
}
