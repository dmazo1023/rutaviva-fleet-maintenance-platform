using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderStatusChangedV1(ServiceOrderSnapshot Order, string FromStatus, string ToStatus, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderStatusChanged";

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
