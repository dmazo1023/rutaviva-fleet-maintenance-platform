using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal interface IEventDispatchStrategy
{
    Task<Result> ExecuteAsync(IServiceProvider services, IntegrationEventEnvelope envelope, EventMetadata metadata, CancellationToken cancellationToken);
}
