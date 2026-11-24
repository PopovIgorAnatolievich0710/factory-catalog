using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.Balancer.Business.Abstractions.Allocations;

namespace Tenengroup.Balancer.CompositionRoot;

public static class HangfireModule
{
    public const string SchemaName = "hangfire_balancer";

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
                    QueuePollInterval = TimeSpan.FromSeconds(5),
                    PrepareSchemaIfNecessary = true,
                    SchemaName = SchemaName
                }));

        services.AddHangfireServer(options => options.Queues = ["default"]);

        return services;
    }
}

public class HangfireAllocationOutputQueue(IBackgroundJobClient backgroundJobs) : IAllocationOutputQueue
{
    public void Enqueue(Guid allocationId)
    {
        backgroundJobs.Enqueue<IAllocationOutputJob>(job => job.Publish(allocationId, CancellationToken.None));
    }
}
