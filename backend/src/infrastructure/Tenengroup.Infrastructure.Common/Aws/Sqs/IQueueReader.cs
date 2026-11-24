namespace Tenengroup.Infrastructure.Common.Aws.Sqs;

public interface IQueueReader
{
    Task ConsumeMessageCollection(
        string queueUrl,
        int maxMessageCount,
        string eventTypeAttribute,
        Func<EventMessageValue, CancellationToken, Task<bool>> handlerAction,
        int? visibilityTimeout = null,
        CancellationToken cancellationToken = default);
}
