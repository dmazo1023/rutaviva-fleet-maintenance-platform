using RutaViva.BuildingBlocks.Domain.Guards;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class EventCheckpoint
{
    public const int SubscriberNameMaxLength = 60;

    public EventCheckpoint(string subscriberName, long lastOffset, DateTimeOffset updatedAt)
    {
        SubscriberName = Guard.NotNullOrWhiteSpace(subscriberName);
        LastOffset = lastOffset;
        UpdatedAt = Guard.NotDefault(updatedAt);
    }

    public string SubscriberName { get; private set; }

    public long LastOffset { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void MoveTo(long offset, DateTimeOffset updatedAt)
    {
        LastOffset = offset;
        UpdatedAt = Guard.NotDefault(updatedAt);
    }
}
