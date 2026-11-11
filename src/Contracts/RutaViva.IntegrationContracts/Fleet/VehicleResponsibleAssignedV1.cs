using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Fleet;

public sealed record VehicleResponsibleAssignedV1(Guid VehicleId, Guid? ResponsibleApproverId, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "VehicleResponsibleAssigned";

    public const string Stream = Streams.FleetVehicle;
}
