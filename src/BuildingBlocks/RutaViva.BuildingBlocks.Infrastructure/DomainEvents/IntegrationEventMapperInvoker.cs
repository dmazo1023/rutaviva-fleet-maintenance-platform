using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

internal interface IIntegrationEventMapperInvoker
{
    IReadOnlyCollection<IIntegrationEvent> Map(IServiceProvider serviceProvider, IDomainEvent domainEvent);
}

internal sealed class IntegrationEventMapperInvoker<TEvent> : IIntegrationEventMapperInvoker
    where TEvent : IDomainEvent
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IServiceProvider serviceProvider, IDomainEvent domainEvent)
        => serviceProvider.GetServices<IIntegrationEventMapper<TEvent>>()
            .SelectMany(mapper => mapper.Map((TEvent)domainEvent))
            .ToList();
}
