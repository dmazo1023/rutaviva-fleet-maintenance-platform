using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Abstractions;

public sealed record OdometerReadingLogEntry(
    VehicleId VehicleId,
    long Kilometers,
    DateTimeOffset ReadAt,
    DateTimeOffset ReceivedAt,
    OdometerReadingOutcome Outcome,
    OdometerReadingIgnoredReason? IgnoredReason);
