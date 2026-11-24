namespace Tenengroup.Infrastructure.Common.Aws.Sns;

public interface IEventPublisher
{
    Task<string> Publish<T>(string topicArn, string eventType, T payload, CancellationToken cancellationToken = default);
}
