using System.Text.Json;
using Amazon.SQS.Model;

namespace Tenengroup.Infrastructure.Common.Aws.Sqs;

public class MessageReader : IMessageReader
{
    public const string CorrelationIdAttribute = "CorrelationId";
    public const string SentTimestampAttribute = "SentTimestamp";

    public EventMessageValue ReadMessage(Message message, string eventTypeAttribute)
    {
        var value = new EventMessageValue
        {
            MessageId = message.MessageId,
            DataJson = message.Body,
            EventType = ReadAttribute(message, eventTypeAttribute) ?? string.Empty,
            CorrelationId = ReadAttribute(message, CorrelationIdAttribute),
            SentAt = ReadSentAt(message)
        };

        if (TryReadEnvelope(message.Body, out var envelope))
        {
            value.DataJson = envelope.GetProperty(nameof(EventMessageValue.DataJson)).GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value.EventType) &&
                envelope.TryGetProperty(nameof(EventMessageValue.EventType), out var eventType))
            {
                value.EventType = eventType.GetString() ?? string.Empty;
            }
        }

        return value;
    }

    private static bool TryReadEnvelope(string body, out JsonElement envelope)
    {
        envelope = default;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(nameof(EventMessageValue.DataJson), out _))
            {
                return false;
            }

            envelope = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static DateTime? ReadSentAt(Message message)
    {
        return message.Attributes != null
               && message.Attributes.TryGetValue(SentTimestampAttribute, out var value)
               && long.TryParse(value, out var milliseconds)
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime
            : null;
    }

    private static string? ReadAttribute(Message message, string name)
    {
        return message.MessageAttributes != null && message.MessageAttributes.TryGetValue(name, out var attribute)
            ? attribute.StringValue
            : null;
    }
}
