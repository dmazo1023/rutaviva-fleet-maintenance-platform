using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

internal interface IDomainEventHandlerInvoker
{
    Task InvokeAsync(IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken);
}

internal sealed class DomainEventHandlerInvoker<TEvent> : IDomainEventHandlerInvoker
    where TEvent : IDomainEvent
{
    public async Task InvokeAsync(IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        foreach (IDomainEventHandler<TEvent> handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
        {
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
        }
    }
}
