using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record MaintenancePlanStatusChangedV1(Guid VehicleId, string Plate, Guid CustomerId, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "MaintenancePlanStatusChanged";

    public const string Stream = Streams.MaintenanceRoutine;
}
