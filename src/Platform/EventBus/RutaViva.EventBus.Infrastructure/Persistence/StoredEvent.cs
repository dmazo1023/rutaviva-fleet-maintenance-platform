using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class StoredEvent
{
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

    public static StoredEvent Create(string stream, IntegrationEventEnvelope envelope, string payload, DateTimeOffset storedAt)
    {
        Guard.NotNull(envelope);
        return new StoredEvent
        {
            MessageId = Guard.NotDefault(envelope.Id),
            Stream = BoundedText.Require(stream, IntegrationEventLimits.StreamMaxLength),
            Type = BoundedText.Require(envelope.Type, IntegrationEventLimits.TypeMaxLength),
            Subject = BoundedText.Require(envelope.Subject, IntegrationEventLimits.SubjectMaxLength),
            OccurredAt = Guard.NotDefault(envelope.Time),
            StoredAt = Guard.NotDefault(storedAt),
            Payload = Guard.NotNullOrWhiteSpace(payload),
        };
    }
}
