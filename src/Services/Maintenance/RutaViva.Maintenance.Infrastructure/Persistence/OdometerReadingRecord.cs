using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence;

public sealed class OdometerReadingRecord
{
    public long Id { get; init; }

    public required VehicleId VehicleId { get; init; }

    public required long Kilometers { get; init; }

    public required DateTimeOffset ReadAt { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public required OdometerReadingOutcome Outcome { get; init; }

    public OdometerReadingIgnoredReason? IgnoredReason { get; init; }
}
