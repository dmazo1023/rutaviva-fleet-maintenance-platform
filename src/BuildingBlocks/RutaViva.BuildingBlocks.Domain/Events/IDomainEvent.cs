namespace RutaViva.BuildingBlocks.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
