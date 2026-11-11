using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineDueStatusRevertedV1(RoutineSnapshot Routine, string Reason, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineDueStatusReverted";

    public const string Stream = Streams.MaintenanceRoutine;
}
