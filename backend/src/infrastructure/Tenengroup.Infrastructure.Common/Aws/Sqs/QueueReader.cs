using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace Tenengroup.Infrastructure.Common.Aws.Sqs;

public class QueueReader(IAmazonSQS sqsClient, IMessageReader messageReader, ILogger<QueueReader> logger) : IQueueReader
{
    private const int WaitTimeSeconds = 20;

    public async Task ConsumeMessageCollection(
        string queueUrl,
        int maxMessageCount,
        string eventTypeAttribute,
        Func<EventMessageValue, CancellationToken, Task<bool>> handlerAction,
        int? visibilityTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            WaitTimeSeconds = WaitTimeSeconds,
            MaxNumberOfMessages = maxMessageCount,
            MessageAttributeNames = ["All"],
            MessageSystemAttributeNames = [MessageReader.SentTimestampAttribute]
        };

        if (visibilityTimeout.HasValue)
        {
            request.VisibilityTimeout = visibilityTimeout.Value;
        }

        ReceiveMessageResponse response;
        try
        {
            response = await sqsClient.ReceiveMessageAsync(request, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to receive messages from queue {QueueUrl}", queueUrl);
            return;
        }

        foreach (var message in response.Messages ?? [])
        {
            await ProcessMessage(queueUrl, message, eventTypeAttribute, handlerAction, cancellationToken);
        }
    }

    private async Task ProcessMessage(
        string queueUrl,
        Message message,
        string eventTypeAttribute,
        Func<EventMessageValue, CancellationToken, Task<bool>> handlerAction,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = messageReader.ReadMessage(message, eventTypeAttribute);

            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = value.CorrelationId ?? Guid.NewGuid().ToString()
            });

            if (string.IsNullOrWhiteSpace(value.EventType))
            {
                logger.LogError("Missing event type in message {MessageId} from queue {QueueUrl}. Body: {Body}",
                    message.MessageId, queueUrl, message.Body);
                return;
            }

            if (await handlerAction(value, cancellationToken))
            {
                await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            }
            else
            {
                logger.LogInformation("Message {MessageId} ({EventType}) was not processed and remains in queue {QueueUrl}",
                    message.MessageId, value.EventType, queueUrl);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to process message {MessageId} from queue {QueueUrl}. Body: {Body}",
                message.MessageId, queueUrl, message.Body);
        }
    }
}
