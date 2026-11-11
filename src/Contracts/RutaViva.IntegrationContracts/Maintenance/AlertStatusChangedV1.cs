using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record AlertStatusChangedV1(Guid AlertId, Guid VehicleId, string Plate, Guid CustomerId, Guid RoutineId, string Type, string TriggeredBy, string Status, DateTimeOffset? AcknowledgedAt, string? AcknowledgedBy, DateTimeOffset? ResolvedAt, string? ResolutionReason, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "AlertStatusChanged";

    public const int Version = 1;

    public const string Stream = Streams.MaintenanceAlert;
}
