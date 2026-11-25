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
    IOcsPaidOrderMapper mapper,
    OrderItemsOptions options,
    ILogger<OrderQueueHandler> logger) : IOrderQueueHandler
{
    private const int MaxMessages = 10;

    public Task Execute(CancellationToken cancellationToken)
    {
        return queueReader.ConsumeMessageCollection(
            options.QueueUrl,
            MaxMessages,
            BalancerEventTypes.OcsEventTypeAttribute,
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

        var command = await mapper.Map(message.DataJson.FromJsonString<OcsPaidOrderMessage>(), cancellationToken);
        if (command == null)
        {
            return true;
        }

        await requestRunner.Run<AllocateOrderCommand, IReadOnlyCollection<AllocationDto>>(command, cancellationToken);

        return true;
    }
}

public class SupplierQueueHandler(
    IQueueReader queueReader,
    IRequestRunner requestRunner,
    SupplierEventsOptions options,
    TimeProvider timeProvider,
    ILogger<SupplierQueueHandler> logger) : ISupplierQueueHandler
{
    private const int MaxMessages = 10;

    public Task Execute(CancellationToken cancellationToken)
    {
        return queueReader.ConsumeMessageCollection(
            options.QueueUrl,
            MaxMessages,
            BalancerEventTypes.SupplierEventTypeAttribute,
            Handle,
            cancellationToken: cancellationToken);
    }

    private Task<bool> Handle(EventMessageValue message, CancellationToken cancellationToken)
    {
        if (message.EventType == BalancerEventTypes.SupplierItemSent)
        {
            var sent = message.DataJson.FromJsonString<SupplierItemSentMessage>();

            return requestRunner.Run<RecordProductionCommand, bool>(
                new RecordProductionCommand(
                    message.MessageId,
                    sent.OrderId.ToString(),
                    sent.ItemId.ToString(),
                    message.SentAt ?? timeProvider.GetUtcNow().UtcDateTime,
                    []),
                cancellationToken);
        }

        if (message.EventType == BalancerEventTypes.SupplierItemProductionStatusChanged)
        {
            var changed = message.DataJson.FromJsonString<SupplierItemProductionStatusChangedMessage>();
            if (string.Equals(changed.ProductionStatus, BalancerEventTypes.SupplierReproductionStatus, StringComparison.OrdinalIgnoreCase))
            {
                return requestRunner.Run<MarkReproductionRequestedCommand, bool>(
                    new MarkReproductionRequestedCommand(changed.OrderId.ToString(), changed.ItemId.ToString(), changed.ChangedAt),
                    cancellationToken);
            }

            return Task.FromResult(true);
        }

        logger.LogInformation("Supplier queue message {MessageId} of type {EventType} is skipped", message.MessageId, message.EventType);
        return Task.FromResult(true);
    }
}
