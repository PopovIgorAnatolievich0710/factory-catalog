using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Infrastructure.Common.Aws.Sns;

public class SnsEventPublisher(IAmazonSimpleNotificationService snsClient, ILogger<SnsEventPublisher> logger) : IEventPublisher
{
    public const string EventTypeAttribute = "EventType";

    public async Task<string> Publish<T>(string topicArn, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            throw new ArgumentException($"Cannot publish event {eventType}: topic ARN is empty.", nameof(topicArn));
        }

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = payload.ToJsonString(),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [EventTypeAttribute] = new() { DataType = "String", StringValue = eventType }
            }
        };

        var response = await snsClient.PublishAsync(request, cancellationToken);
        logger.LogInformation("{EventType} event {MessageId} is published to topic {TopicArn}", eventType, response.MessageId, topicArn);

        return response.MessageId;
    }
}
