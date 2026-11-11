using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<Result> HandleAsync(TEvent integrationEvent, EventMetadata metadata, CancellationToken cancellationToken);
}
