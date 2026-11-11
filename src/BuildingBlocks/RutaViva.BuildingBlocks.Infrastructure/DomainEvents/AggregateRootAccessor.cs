using System.Collections.Concurrent;
using System.Reflection;
using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

internal sealed class AggregateRootAccessor
{
    private const string NotAggregateRootMessage = "The type '{0}' does not inherit from AggregateRoot<TId>.";

    private static readonly ConcurrentDictionary<Type, AggregateRootAccessor> Cache = new();

    private readonly PropertyInfo _domainEvents;
    private readonly PropertyInfo _id;
    private readonly MethodInfo _clearDomainEvents;

    private AggregateRootAccessor(Type closedType)
    {
        _domainEvents = closedType.GetProperty(nameof(AggregateRoot<object>.DomainEvents))!;
        _id = closedType.GetProperty(nameof(Entity<object>.Id))!;
        _clearDomainEvents = closedType.GetMethod(nameof(AggregateRoot<object>.ClearDomainEvents))!;
    }

    public static AggregateRootAccessor For(Type aggregateType) => Cache.GetOrAdd(aggregateType, Create);

    public List<IDomainEvent> ReadEvents(object aggregate)
    {
        IReadOnlyCollection<IDomainEvent> events = (IReadOnlyCollection<IDomainEvent>)_domainEvents.GetValue(aggregate)!;
        return [.. events];
    }

    public string ReadSubject(object aggregate) => _id.GetValue(aggregate)?.ToString() ?? string.Empty;

    public void ClearEvents(object aggregate) => _clearDomainEvents.Invoke(aggregate, null);

    private static AggregateRootAccessor Create(Type aggregateType)
    {
        Type closedType = AggregateRootTypes.FindClosedType(aggregateType)
            ?? throw new InvalidOperationException(string.Format(NotAggregateRootMessage, aggregateType.Name));
        return new AggregateRootAccessor(closedType);
    }
}
