using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record Odometer : ValueObject
{
    private Odometer(long kilometers, DateTimeOffset readAt)
    {
        Kilometers = kilometers;
        ReadAt = readAt;
    }

    public long Kilometers { get; }

    public DateTimeOffset ReadAt { get; }

    public static Result<Odometer> Create(long kilometers, DateTimeOffset readAt)
    {
        if (kilometers < 0)
        {
            return MaintenanceErrors.OdometerKilometersNegative;
        }

        return new Odometer(kilometers, readAt);
    }

    public bool IsAdvancedBy(OdometerReading reading) => reading.Kilometers > Kilometers;

    public Odometer Advance(OdometerReading reading)
    {
        if (!IsAdvancedBy(reading))
        {
            return this;
        }

        return new Odometer(reading.Kilometers, reading.ReadAt);
    }
}
