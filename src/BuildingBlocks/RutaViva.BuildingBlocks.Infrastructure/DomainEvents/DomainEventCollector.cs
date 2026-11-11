using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed class DomainEventCollector
{
    public IReadOnlyList<CollectedDomainEvent> CollectAndClear(DbContext context)
    {
        List<CollectedDomainEvent> collected = [];
        foreach (object aggregate in TrackedAggregates(context))
        {
            collected.AddRange(CollectAndClear(aggregate));
        }

        return collected;
    }

    public bool HasPending(DbContext context)
        => TrackedAggregates(context).Any(aggregate => AggregateRootAccessor.For(aggregate.GetType()).ReadEvents(aggregate).Count > 0);

    private static List<object> TrackedAggregates(DbContext context)
        => context.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .Where(entity => AggregateRootTypes.IsAggregateRoot(entity.GetType()))
            .ToList();

    private static List<CollectedDomainEvent> CollectAndClear(object aggregate)
    {
        AggregateRootAccessor accessor = AggregateRootAccessor.For(aggregate.GetType());
        List<IDomainEvent> events = accessor.ReadEvents(aggregate);
        string subject = accessor.ReadSubject(aggregate);
        accessor.ClearEvents(aggregate);
        return events.Select(domainEvent => new CollectedDomainEvent(subject, domainEvent)).ToList();
    }
}
