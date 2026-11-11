using RutaViva.IntegrationContracts.Maintenance;

namespace RutaViva.IntegrationContracts.Abstractions;

public interface IRoutineSnapshotEvent
{
    RoutineSnapshot Routine { get; }
}
