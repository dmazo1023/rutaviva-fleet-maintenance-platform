using RutaViva.IntegrationContracts.ServiceOrders;

namespace RutaViva.IntegrationContracts.Abstractions;

public interface IServiceOrderSnapshotEvent
{
    ServiceOrderSnapshot Order { get; }
}
