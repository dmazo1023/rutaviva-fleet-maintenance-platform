using RutaViva.BuildingBlocks.Domain.Guards;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public const int StreamMaxLength = 60;
    public const int EventTypeMaxLength = 150;
    public const int SubjectMaxLength = 60;
    public const int LastErrorMaxLength = 1000;

    public OutboxMessage(Guid id, string stream, string eventType, string subject, string payload, DateTimeOffset occurredAt)
    {
        Id = Guard.NotDefault(id);
        Stream = Guard.NotNullOrWhiteSpace(stream);
        EventType = Guard.NotNullOrWhiteSpace(eventType);
        Subject = Guard.NotNullOrWhiteSpace(subject);
        Payload = Guard.NotNullOrWhiteSpace(payload);
        OccurredAt = Guard.NotDefault(occurredAt);
    }

    public Guid Id { get; private set; }

    public string Stream { get; private set; }

    public string EventType { get; private set; }

    public string Subject { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = Guard.NotDefault(processedAt);
    }

    public void RegisterFailure(string error)
    {
        string validated = Guard.NotNullOrWhiteSpace(error);
        Attempts++;
        LastError = Truncate(validated);
    }

    private static string Truncate(string error)
        => error.Length <= LastErrorMaxLength ? error : error[..LastErrorMaxLength];
}
