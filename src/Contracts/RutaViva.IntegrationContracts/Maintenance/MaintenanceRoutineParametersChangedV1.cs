using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineParametersChangedV1(RoutineSnapshot Routine, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineParametersChanged";

    public const string Stream = Streams.MaintenanceRoutine;
}
