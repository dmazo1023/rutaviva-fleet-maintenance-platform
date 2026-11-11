using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderAppointmentScheduledV1(ServiceOrderSnapshot Order, DateTimeOffset ScheduledAt, string? Notes, bool IsReschedule, DateTimeOffset OccurredAt) : IIntegrationEvent, IServiceOrderSnapshotEvent
{
    public const string EventName = "ServiceOrderAppointmentScheduled";

    public const string Stream = Streams.ServiceOrdersServiceOrder;
}
