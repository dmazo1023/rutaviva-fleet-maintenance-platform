using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineOverdueV1(RoutineSnapshot Routine, string TriggeredBy, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineOverdue";

    public const string Stream = Streams.MaintenanceRoutine;
}
