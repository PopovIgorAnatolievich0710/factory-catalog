using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tenengroup.Infrastructure.Common.Permissions;

public class GroupPermissionsProvider(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<GroupPermissionsProvider> logger) : IGroupPermissionsProvider
{
    public const string HttpClientName = "GroupPermissionsClient";
    private const string CacheKey = "group_permissions_key";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(20);

    public async Task<GroupPermissions?> Get(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<GroupPermissions>(CacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var url = configuration[Constants.ParameterNames.GroupPermissionsUrl];
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogError("{Param} is not configured; group permissions unavailable", Constants.ParameterNames.GroupPermissionsUrl);
            return null;
        }

        try
        {
            var fetched = await httpClientFactory.CreateClient(HttpClientName)
                .GetFromJsonAsync<GroupPermissions>(url, cancellationToken) ?? new GroupPermissions();
            fetched.RoleGroupMapping ??= new Dictionary<string, string[]>();
            fetched.RestrictedGroups ??= [];

            cache.Set(CacheKey, fetched, CacheDuration);
            return fetched;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch group permissions from {Url}", url);
            return null;
        }
    }
}
