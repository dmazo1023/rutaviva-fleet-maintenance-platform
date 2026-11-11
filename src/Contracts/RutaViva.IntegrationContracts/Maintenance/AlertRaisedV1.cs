using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record AlertRaisedV1(Guid AlertId, Guid VehicleId, string Plate, Guid CustomerId, Guid RoutineId, string RoutineName, int CycleNumber, string Type, string TriggeredBy, string Status, DateTimeOffset RaisedAt, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "AlertRaised";

    public const string Stream = Streams.MaintenanceAlert;
}
