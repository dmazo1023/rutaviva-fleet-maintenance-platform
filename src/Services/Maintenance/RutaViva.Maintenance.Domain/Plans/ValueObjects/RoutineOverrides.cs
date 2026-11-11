using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record RoutineOverrides : ValueObject
{
    public static readonly RoutineOverrides None = new(null, null, null);

    private RoutineOverrides(int? intervalKm, int? intervalMonths, decimal? dueSoonPercent)
    {
        IntervalKm = intervalKm;
        IntervalMonths = intervalMonths;
        DueSoonPercent = dueSoonPercent;
    }

    public int? IntervalKm { get; }

    public int? IntervalMonths { get; }

    public decimal? DueSoonPercent { get; }

    public static Result<RoutineOverrides> Create(int? intervalKm, int? intervalMonths, decimal? dueSoonPercent)
    {
        Error? invalid = MaintenanceInterval.ValidateKilometers(intervalKm)
            ?? MaintenanceInterval.ValidateMonths(intervalMonths)
            ?? ValidatePercent(dueSoonPercent);
        if (invalid is not null)
        {
            return invalid;
        }

        return new RoutineOverrides(intervalKm, intervalMonths, dueSoonPercent);
    }

    private static Error? ValidatePercent(decimal? percent)
    {
        if (percent is { } value && !DueSoonThreshold.IsValid(value))
        {
            return MaintenanceErrors.DueSoonThresholdOutOfRange;
        }

        return null;
    }
}
