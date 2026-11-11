using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineStateChangedV1(RoutineSnapshot Routine, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineStateChanged";

    public const int Version = 1;

    public const string Stream = Streams.MaintenanceRoutine;
}
