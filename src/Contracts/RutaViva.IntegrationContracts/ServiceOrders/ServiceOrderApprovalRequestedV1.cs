using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderApprovalRequestedV1(ServiceOrderSnapshot Order, string Reason, string Priority, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderApprovalRequested";

    public const int Version = 1;

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
