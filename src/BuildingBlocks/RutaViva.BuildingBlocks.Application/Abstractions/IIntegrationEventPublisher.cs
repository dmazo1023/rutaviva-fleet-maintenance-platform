using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent, string subject);
}
