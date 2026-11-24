using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Shared.FeatureToggles;

public class FeatureToggleService(IFactoryCatalogDbContext db, IMemoryCache cache)
    : IFeatureToggleService, IPermissionEnforcementToggle
{
    internal const string CacheKey = "feature_toggle_list_key";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyCollection<FeatureToggle>> GetAll(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<IReadOnlyCollection<FeatureToggle>>(CacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var toggles = await db.FeatureToggles
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToArrayAsync(cancellationToken);

        cache.Set(CacheKey, toggles, CacheExpiration);
        return toggles;
    }

    public async Task<bool> IsEnabled(string name, CancellationToken cancellationToken = default)
    {
        var toggles = await GetAll(cancellationToken);
        return toggles.FirstOrDefault(t => t.Name == name)?.IsEnabled ?? false;
    }

    public Task<bool> IsEnforced(CancellationToken cancellationToken = default)
    {
        return IsEnabled(FeatureToggleNames.PermissionsEnableAll, cancellationToken);
    }
}
