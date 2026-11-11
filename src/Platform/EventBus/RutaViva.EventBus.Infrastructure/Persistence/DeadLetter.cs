using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class DeadLetter
{
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
        SubscriberName = BoundedText.Require(subscriberName, IntegrationEventLimits.SubscriberNameMaxLength),
        Offset = offset,
        Error = BoundedText.Require(error, IntegrationEventLimits.FailureMaxLength),
        ReportedAt = Guard.NotDefault(reportedAt),
    };
}
