using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class StoredEvent
{
    public const int StreamMaxLength = 60;
    public const int TypeMaxLength = 150;
    public const int SubjectMaxLength = 60;

    private StoredEvent()
    {
    }

    public long Offset { get; private set; }

    public Guid MessageId { get; private set; }

    public string Stream { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset StoredAt { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public static StoredEvent Create(string stream, IntegrationEventEnvelope envelope, string payload, DateTimeOffset storedAt) => new()
    {
        MessageId = envelope.Id,
        Stream = stream,
        Type = envelope.Type,
        Subject = envelope.Subject,
        OccurredAt = envelope.Time,
        StoredAt = storedAt,
        Payload = payload,
    };
}
