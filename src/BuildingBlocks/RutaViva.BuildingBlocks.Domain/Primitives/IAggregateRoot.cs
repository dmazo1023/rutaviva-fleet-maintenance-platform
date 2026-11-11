using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.Domain.Primitives;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
