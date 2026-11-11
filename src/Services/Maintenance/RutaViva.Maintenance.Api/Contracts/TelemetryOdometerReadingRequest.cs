namespace RutaViva.Maintenance.Api.Contracts;

public sealed record TelemetryOdometerReadingRequest(string? Plate, long OdometerKm, DateTimeOffset ReadAt);
