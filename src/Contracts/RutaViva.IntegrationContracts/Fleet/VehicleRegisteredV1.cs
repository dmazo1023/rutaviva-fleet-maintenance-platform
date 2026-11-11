using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Fleet;

public sealed record VehicleRegisteredV1(Guid VehicleId, string Plate, string? Vin, string Brand, string Model, int ModelYear, Guid CustomerId, Guid WorkshopId, Guid? ResponsibleApproverId, string Status, long InitialOdometerKm, DateTimeOffset RegisteredAt, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "VehicleRegistered";

    public const int Version = 1;

    public const string Stream = Streams.FleetVehicle;
}
