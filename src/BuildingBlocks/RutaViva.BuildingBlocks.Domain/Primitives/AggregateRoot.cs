using RutaViva.BuildingBlocks.Domain.Guards;
using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.Domain.Primitives;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    void IAggregateRoot.ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent)
    {
        Guard.NotNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
