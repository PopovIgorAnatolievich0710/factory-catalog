using Amazon.SQS.Model;

namespace Tenengroup.Infrastructure.Common.Aws.Sqs;

public interface IMessageReader
{
    EventMessageValue ReadMessage(Message message, string eventTypeAttribute);
}
