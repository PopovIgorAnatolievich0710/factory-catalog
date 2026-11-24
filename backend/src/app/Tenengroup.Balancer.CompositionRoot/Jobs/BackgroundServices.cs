using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Balancer.Business.Abstractions.Shipping;

namespace Tenengroup.Balancer.CompositionRoot.Jobs;

public class RecurringJobsHostedService(IRecurringJobManager recurringJobs, BalancerOptions options) : IHostedService
{
    public const string ShippingRouteRefreshJobId = "balancer-shipping-route-refresh";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobs.AddOrUpdate<IShippingRouteRefreshJob>(
            ShippingRouteRefreshJobId,
            job => job.Run(CancellationToken.None),
            options.ShippingRoutesCronExpression);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public abstract class QueueConsumerService(
    IServiceScopeFactory scopeFactory,
    string queueUrl,
    int pollSeconds,
    ILogger logger) : BackgroundService
{
    protected abstract Task Consume(IServiceProvider scopedServices, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning("{Service} is disabled: the queue URL is not configured", GetType().Name);
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(1, pollSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    await Consume(scope.ServiceProvider, stoppingToken);
                }

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "{Service} iteration failed", GetType().Name);
                await Task.Delay(delay, CancellationToken.None);
            }
        }
    }
}

public class OrderQueueConsumerService(
    IServiceScopeFactory scopeFactory,
    BalancerOptions options,
    ILogger<OrderQueueConsumerService> logger)
    : QueueConsumerService(scopeFactory, options.OrderQueueUrl, options.QueuePollSeconds, logger)
{
    protected override Task Consume(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        return scopedServices.GetRequiredService<IOrderQueueHandler>().Execute(cancellationToken);
    }
}

public class SupplierQueueConsumerService(
    IServiceScopeFactory scopeFactory,
    BalancerOptions options,
    ILogger<SupplierQueueConsumerService> logger)
    : QueueConsumerService(scopeFactory, options.SupplierQueueUrl, options.QueuePollSeconds, logger)
{
    protected override Task Consume(IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        return scopedServices.GetRequiredService<ISupplierQueueHandler>().Execute(cancellationToken);
    }
}
