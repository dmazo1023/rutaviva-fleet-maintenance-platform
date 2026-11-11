using RutaViva.IntegrationContracts.Maintenance;

namespace RutaViva.IntegrationContracts.Abstractions;

public interface IRoutineSnapshotEvent : IIntegrationEvent
{
    RoutineSnapshot Routine { get; }
}
