using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record OdometerReadingRecordedV1(Guid VehicleId, string Plate, Guid CustomerId, long Kilometers, DateTimeOffset ReadAt, string Outcome, string? IgnoredReason, long CurrentOdometerKm, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "OdometerReadingRecorded";

    public const int Version = 1;

    public const string Stream = Streams.MaintenanceRoutine;
}
