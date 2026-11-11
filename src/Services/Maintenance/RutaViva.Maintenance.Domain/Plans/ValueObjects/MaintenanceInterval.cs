using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record MaintenanceInterval : ValueObject
{
    public const int MaxMonths = 1200;

    private MaintenanceInterval(int? kilometers, int? months)
    {
        Kilometers = kilometers;
        Months = months;
    }

    public int? Kilometers { get; }

    public int? Months { get; }

    public bool HasDistance => Kilometers.HasValue;

    public bool HasTime => Months.HasValue;

    public static Result<MaintenanceInterval> Create(int? kilometers, int? months)
    {
        if (kilometers is null && months is null)
        {
            return MaintenanceErrors.MaintenanceIntervalEmpty;
        }

        Error? invalid = ValidateKilometers(kilometers) ?? ValidateMonths(months);
        if (invalid is not null)
        {
            return invalid;
        }

        return new MaintenanceInterval(kilometers, months);
    }

    internal static Error? ValidateKilometers(int? kilometers)
    {
        if (kilometers <= 0)
        {
            return MaintenanceErrors.MaintenanceIntervalKilometersNotPositive;
        }

        return null;
    }

    internal static Error? ValidateMonths(int? months)
    {
        if (months <= 0)
        {
            return MaintenanceErrors.MaintenanceIntervalMonthsNotPositive;
        }

        if (months > MaxMonths)
        {
            return MaintenanceErrors.MaintenanceIntervalMonthsTooLarge;
        }

        return null;
    }
}
