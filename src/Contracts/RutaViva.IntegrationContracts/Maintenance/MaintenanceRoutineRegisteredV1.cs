using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineRegisteredV1(RoutineSnapshot Routine, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineRegistered";

    public const int Version = 1;

    public const string Stream = Streams.MaintenanceRoutine;
}
