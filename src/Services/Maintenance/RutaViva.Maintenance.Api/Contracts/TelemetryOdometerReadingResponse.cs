using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Api.Contracts;

public sealed record TelemetryOdometerReadingResponse(OdometerReadingOutcome Outcome, OdometerReadingIgnoredReason? Reason);
