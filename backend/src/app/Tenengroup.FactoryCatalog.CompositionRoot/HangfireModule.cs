using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.FactoryCatalog.CompositionRoot;

public static class HangfireModule
{
    public const string SchemaName = "hangfire";

    public static IServiceCollection AddHangfireModule(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(10),
                    PrepareSchemaIfNecessary = true,
                    SchemaName = SchemaName
                }));

        services.AddHangfireServer(options => options.Queues = ["default"]);

        return services;
    }
}
