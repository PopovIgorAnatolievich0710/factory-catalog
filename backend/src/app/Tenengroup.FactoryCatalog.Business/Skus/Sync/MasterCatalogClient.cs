using System.Globalization;
using System.Net.Http.Json;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.FactoryCatalog.Business.Skus.Sync;

public class MasterCatalogClient(HttpClient httpClient) : IMasterCatalogClient
{
    public const string SkusPath = "v1/skus";

    public async Task<MasterCatalogSkuPage> GetChangedSkus(DateTime? changedSince, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("Master catalog base URL (MasterCatalog:BaseUrl) is not configured");
        }

        var url = $"{SkusPath}?page={page}&pageSize={pageSize}";
        if (changedSince != null)
        {
            var since = changedSince.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            url += $"&changedSince={Uri.EscapeDataString(since)}";
        }

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MasterCatalogSkuPage>(JsonExtensions.Options, cancellationToken)
               ?? new MasterCatalogSkuPage([], false);
    }
}
