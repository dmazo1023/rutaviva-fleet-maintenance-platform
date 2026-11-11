using RutaViva.IntegrationContracts.ServiceOrders;

namespace RutaViva.IntegrationContracts.Abstractions;

public interface IServiceOrderSnapshotEvent : IIntegrationEvent
{
    ServiceOrderSnapshot Order { get; }
}
