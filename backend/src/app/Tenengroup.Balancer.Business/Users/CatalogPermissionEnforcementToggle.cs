using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Business.Users;

public class CatalogPermissionEnforcementToggle(
    ICatalogClient catalogClient,
    IMemoryCache cache,
    ILogger<CatalogPermissionEnforcementToggle> logger) : IPermissionEnforcementToggle
{
    internal const string CacheKey = "balancer_permission_enforcement";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    public async Task<bool> IsEnforced(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<bool>(CacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var enforced = await catalogClient.IsPermissionEnforcementEnabled(cancellationToken);
            cache.Set(CacheKey, enforced, CacheExpiration);
            return enforced;
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Permission toggle could not be read from the factory catalog; permissions are enforced");
            return true;
        }
    }
}
