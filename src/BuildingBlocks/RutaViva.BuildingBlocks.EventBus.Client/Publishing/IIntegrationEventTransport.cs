using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public interface IIntegrationEventTransport
{
    Task<long> SendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}
