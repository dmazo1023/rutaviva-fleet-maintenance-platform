using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderLinesChangedV1(ServiceOrderSnapshot Order, IReadOnlyList<ServiceOrderLineSnapshot> Lines, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderLinesChanged";

    public const int Version = 1;

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
