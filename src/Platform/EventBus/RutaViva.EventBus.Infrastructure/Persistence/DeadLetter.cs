namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class DeadLetter
{
    public const int SubscriberNameMaxLength = 60;
    public const int ErrorMaxLength = 2000;

    private DeadLetter()
    {
    }

    public long Id { get; private set; }

    public string SubscriberName { get; private set; } = string.Empty;

    public long Offset { get; private set; }

    public string Error { get; private set; } = string.Empty;

    public DateTimeOffset ReportedAt { get; private set; }

    public static DeadLetter Create(string subscriberName, long offset, string error, DateTimeOffset reportedAt) => new()
    {
        SubscriberName = subscriberName,
        Offset = offset,
        Error = error,
        ReportedAt = reportedAt,
    };
}
