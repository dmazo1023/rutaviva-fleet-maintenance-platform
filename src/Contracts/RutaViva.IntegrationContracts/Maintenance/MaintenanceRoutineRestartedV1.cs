using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenanceRoutineRestartedV1(RoutineSnapshot Routine, Guid ServiceOrderId, long ServicedKm, DateTimeOffset ServicedAt, DateTimeOffset OccurredAt) : IIntegrationEvent, IRoutineSnapshotEvent
{
    public const string EventName = "MaintenanceRoutineRestarted";

    public const string Stream = Streams.MaintenanceRoutine;
}
