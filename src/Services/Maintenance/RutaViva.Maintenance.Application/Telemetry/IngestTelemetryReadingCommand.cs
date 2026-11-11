using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed record IngestTelemetryReadingCommand(string Plate, long OdometerKm, DateTimeOffset ReadAt)
    : ICommand<TelemetryReadingOutcome>;
