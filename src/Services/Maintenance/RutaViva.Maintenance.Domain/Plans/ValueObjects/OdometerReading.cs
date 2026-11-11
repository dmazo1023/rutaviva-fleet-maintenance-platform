using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record OdometerReading : ValueObject
{
    private OdometerReading(long kilometers, DateTimeOffset readAt)
    {
        Kilometers = kilometers;
        ReadAt = readAt;
    }

    public long Kilometers { get; }

    public DateTimeOffset ReadAt { get; }

    public static Result<OdometerReading> Create(long kilometers, DateTimeOffset readAt)
    {
        if (kilometers < 0)
        {
            return MaintenanceErrors.OdometerReadingKilometersNegative;
        }

        return new OdometerReading(kilometers, readAt);
    }
}
