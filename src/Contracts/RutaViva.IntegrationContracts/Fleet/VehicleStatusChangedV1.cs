using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Fleet;

public sealed record VehicleStatusChangedV1(Guid VehicleId, string Plate, Guid CustomerId, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "VehicleStatusChanged";

    public const int Version = 1;

    public const string Stream = Streams.FleetVehicle;
}
