namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed class DomainEventHandlerDispatcher(IServiceProvider serviceProvider)
{
    private static readonly ClosedGenericCache<IDomainEventHandlerInvoker> Invokers = new(typeof(DomainEventHandlerInvoker<>));

    public async Task DispatchAsync(IReadOnlyList<CollectedDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (CollectedDomainEvent collected in events)
        {
            IDomainEventHandlerInvoker invoker = Invokers.For(collected.Event.GetType());
            await invoker.InvokeAsync(serviceProvider, collected.Event, cancellationToken);
        }
    }
}
