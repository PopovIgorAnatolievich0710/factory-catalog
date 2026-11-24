using Microsoft.Extensions.Logging;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Events;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Infrastructure.Common.Aws.Sqs;
using Tenengroup.Infrastructure.Common.Json;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Allocations;

public class OrderQueueHandler(
    IQueueReader queueReader,
    IRequestRunner requestRunner,
    BalancerOptions options,
    ILogger<OrderQueueHandler> logger) : IOrderQueueHandler
{
    private const int MaxMessages = 10;

    public Task Execute(CancellationToken cancellationToken)
    {
        return queueReader.ConsumeMessageCollection(
            options.OrderQueueUrl,
            MaxMessages,
            BalancerEventTypes.EventTypeAttribute,
            Handle,
            cancellationToken: cancellationToken);
    }

    private async Task<bool> Handle(EventMessageValue message, CancellationToken cancellationToken)
    {
        if (message.EventType != BalancerEventTypes.OrderPaid)
        {
            logger.LogInformation("Order queue message {MessageId} of type {EventType} is skipped", message.MessageId, message.EventType);
            return true;
        }

        var command = message.DataJson.FromJsonString<AllocateOrderCommand>();
        await requestRunner.Run<AllocateOrderCommand, IReadOnlyCollection<AllocationDto>>(command, cancellationToken);

        return true;
    }
}

public class SupplierQueueHandler(
    IQueueReader queueReader,
    IRequestRunner requestRunner,
    BalancerOptions options,
    ILogger<SupplierQueueHandler> logger) : ISupplierQueueHandler
{
    private const int MaxMessages = 10;

    public Task Execute(CancellationToken cancellationToken)
    {
        return queueReader.ConsumeMessageCollection(
            options.SupplierQueueUrl,
            MaxMessages,
            BalancerEventTypes.EventTypeAttribute,
            Handle,
            cancellationToken: cancellationToken);
    }

    private Task<bool> Handle(EventMessageValue message, CancellationToken cancellationToken)
    {
        if (message.EventType != BalancerEventTypes.SupplierItemSent
            && message.EventType != BalancerEventTypes.SupplierItemReproduced)
        {
            logger.LogInformation("Supplier queue message {MessageId} of type {EventType} is skipped", message.MessageId, message.EventType);
            return Task.FromResult(true);
        }

        var data = message.DataJson.FromJsonString<SupplierItemEventMessage>();

        return requestRunner.Run<RecordProductionCommand, bool>(
            new RecordProductionCommand(
                message.MessageId,
                data.OrderId,
                data.OrderItemId,
                data.SentAt,
                message.EventType == BalancerEventTypes.SupplierItemReproduced,
                data.ProductionLog ?? []),
            cancellationToken);
    }
}
