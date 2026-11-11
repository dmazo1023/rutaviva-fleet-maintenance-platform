using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class ProcessedEffect
{
    public const int TextMaxLength = 200;

    public ProcessedEffect(Guid messageId, string subject, string eventType, long offset)
    {
        MessageId = messageId;
        Subject = subject;
        EventType = eventType;
        Offset = offset;
    }

    public long Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string Subject { get; private set; }

    public string EventType { get; private set; }

    public long Offset { get; private set; }

    public static ProcessedEffect From(EventMetadata metadata)
        => new(metadata.MessageId, metadata.Subject, metadata.Type, metadata.Offset);
}
