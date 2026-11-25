using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Balancer.Business.Catalog;
using Tenengroup.Balancer.Business.Shipping;

namespace Tenengroup.Balancer.CompositionRoot;

public static class ExternalClientsModule
{
    public const string FactoryCatalogSection = "FactoryCatalog";
    public const string ShipmentServiceSection = "ShipmentService";
    public const string MasterCatalogSection = "MasterCatalog";

    private const int RetryCount = 3;

    public static IServiceCollection AddExternalClients(this IServiceCollection services, IConfiguration configuration)
    {
        var catalog = configuration.GetSection(FactoryCatalogSection);
        var shipment = configuration.GetSection(ShipmentServiceSection);

        services.AddHttpClient<ICatalogClient, CatalogClient>(client => Configure(client, catalog))
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(RetryCount, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));

        services.AddHttpClient<IShippingRouteClient, ShippingRouteClient>(client => Configure(client, shipment))
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(RetryCount, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        services.AddHttpClient<IComponentBreakdownClient, ComponentBreakdownClient>(client =>
                Configure(client, configuration.GetSection(MasterCatalogSection), useBasicAuth: false))
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(RetryCount, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));

        return services;
    }

    private static void Configure(HttpClient client, IConfigurationSection section, bool useBasicAuth = true)
    {
        var baseUrl = section["BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        var username = section["Username"];
        var password = section["Password"];
        if (useBasicAuth && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        }
    }
}
