using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed record TelemetryReadingOutcome(OdometerReadingOutcome Outcome, OdometerReadingIgnoredReason? IgnoredReason);
