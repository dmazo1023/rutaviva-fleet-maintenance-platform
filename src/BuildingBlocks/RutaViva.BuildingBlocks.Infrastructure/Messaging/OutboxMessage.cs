using RutaViva.BuildingBlocks.Domain.Guards;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public const int LastErrorMaxLength = 1000;
    public const int MaxAttempts = 10;

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

    public long Sequence { get; private set; }

    public string Stream { get; private set; }

    public string EventType { get; private set; }

    public string Subject { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = Guard.NotDefault(processedAt);
    }

    public void RegisterFailure(string error, DateTimeOffset at)
    {
        RecordError(error);
        if (Attempts >= MaxAttempts)
        {
            FailedAt = Guard.NotDefault(at);
        }
    }

    public void MarkFailed(string error, DateTimeOffset at)
    {
        RecordError(error);
        FailedAt = Guard.NotDefault(at);
    }

    private void RecordError(string error)
    {
        string validated = Guard.NotNullOrWhiteSpace(error);
        Attempts++;
        LastError = Truncate(validated);
    }

    private static string Truncate(string error)
        => error.Length <= LastErrorMaxLength ? error : error[..LastErrorMaxLength];
}
