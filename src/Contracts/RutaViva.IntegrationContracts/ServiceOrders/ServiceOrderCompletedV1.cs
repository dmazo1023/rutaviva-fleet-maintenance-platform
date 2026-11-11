using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderCompletedV1(ServiceOrderSnapshot Order, long ServicedKm, DateTimeOffset ServicedAt, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderCompleted";

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
