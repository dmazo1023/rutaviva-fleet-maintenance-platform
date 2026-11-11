using System.Collections.Concurrent;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

internal sealed class ClosedGenericCache<TInvoker>(Type openGenericType)
    where TInvoker : class
{
    private readonly ConcurrentDictionary<Type, TInvoker> _invokers = new();

    public TInvoker For(Type eventType) => _invokers.GetOrAdd(eventType, Create);

    private TInvoker Create(Type eventType)
        => (TInvoker)Activator.CreateInstance(openGenericType.MakeGenericType(eventType))!;
}
