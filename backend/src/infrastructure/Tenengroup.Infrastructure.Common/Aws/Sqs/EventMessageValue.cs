namespace Tenengroup.Infrastructure.Common.Aws.Sqs;

public sealed class EventMessageValue
{
    public string EventType { get; set; } = string.Empty;

    public string DataJson { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public DateTime? SentAt { get; set; }
}
