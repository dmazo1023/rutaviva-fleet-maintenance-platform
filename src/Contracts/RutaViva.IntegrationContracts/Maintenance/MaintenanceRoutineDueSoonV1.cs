using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineDueSoonV1(RoutineSnapshot Routine, string TriggeredBy, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineDueSoon";

    public const int Version = 1;

    public const string Stream = Streams.MaintenanceRoutine;
}
