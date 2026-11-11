using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed class DomainEventCollector
{
    private const string IdPropertyName = "Id";

    public IReadOnlyList<CollectedDomainEvent> CollectAndClear(DbContext context)
    {
        List<CollectedDomainEvent> collected = [];
        foreach (EntityEntry<IAggregateRoot> entry in TrackedAggregates(context))
        {
            collected.AddRange(CollectAndClear(entry));
        }

        return collected;
    }

    public bool HasPending(DbContext context)
        => TrackedAggregates(context).Any(entry => entry.Entity.DomainEvents.Count > 0);

    private static List<EntityEntry<IAggregateRoot>> TrackedAggregates(DbContext context)
        => context.ChangeTracker.Entries<IAggregateRoot>().ToList();

    private static List<CollectedDomainEvent> CollectAndClear(EntityEntry<IAggregateRoot> entry)
    {
        IAggregateRoot aggregate = entry.Entity;
        List<IDomainEvent> events = [.. aggregate.DomainEvents];
        string subject = ReadSubject(entry);
        aggregate.ClearDomainEvents();
        return events.Select(domainEvent => new CollectedDomainEvent(subject, domainEvent)).ToList();
    }

    private static string ReadSubject(EntityEntry<IAggregateRoot> entry)
        => entry.Property(IdPropertyName).CurrentValue?.ToString() ?? string.Empty;
}
