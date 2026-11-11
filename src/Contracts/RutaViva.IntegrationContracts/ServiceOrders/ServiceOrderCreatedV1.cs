using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderCreatedV1(ServiceOrderSnapshot Order, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderCreated";

    public const int Version = 1;

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
