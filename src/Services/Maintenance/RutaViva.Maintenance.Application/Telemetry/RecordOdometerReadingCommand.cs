using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed record RecordOdometerReadingCommand(VehicleId VehicleId, long Kilometers, DateTimeOffset ReadAt)
    : ICommand<TelemetryReadingOutcome>;
