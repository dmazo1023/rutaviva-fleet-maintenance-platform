using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
