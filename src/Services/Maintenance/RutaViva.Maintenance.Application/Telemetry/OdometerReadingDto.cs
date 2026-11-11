using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed record OdometerReadingDto(
    long Kilometers,
    DateTimeOffset ReadAt,
    DateTimeOffset ReceivedAt,
    OdometerReadingOutcome Outcome,
    OdometerReadingIgnoredReason? IgnoredReason);
