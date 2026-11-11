using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record EffectiveRoutineParameters : ValueObject
{
    private EffectiveRoutineParameters(
        MaintenanceInterval interval,
        DueSoonThreshold dueSoonThreshold,
        RoutineOverrides overrides)
    {
        Interval = interval;
        DueSoonThreshold = dueSoonThreshold;
        IntervalKilometersSource = SourceOf(overrides.IntervalKm);
        IntervalMonthsSource = SourceOf(overrides.IntervalMonths);
        DueSoonSource = SourceOf(overrides.DueSoonPercent);
    }

    public MaintenanceInterval Interval { get; }

    public DueSoonThreshold DueSoonThreshold { get; }

    public ParameterSource IntervalKilometersSource { get; }

    public ParameterSource IntervalMonthsSource { get; }

    public ParameterSource DueSoonSource { get; }

    public static Result<EffectiveRoutineParameters> Resolve(
        MaintenanceInterval typeInterval,
        DueSoonThreshold typeThreshold,
        RoutineOverrides overrides)
    {
        Result<MaintenanceInterval> interval = MaintenanceInterval.Create(
            overrides.IntervalKm ?? typeInterval.Kilometers,
            overrides.IntervalMonths ?? typeInterval.Months);
        if (interval.IsFailure)
        {
            return interval.Error;
        }

        Result<DueSoonThreshold> threshold = ResolveThreshold(typeThreshold, overrides);
        if (threshold.IsFailure)
        {
            return threshold.Error;
        }

        return new EffectiveRoutineParameters(interval.Value, threshold.Value, overrides);
    }

    private static Result<DueSoonThreshold> ResolveThreshold(DueSoonThreshold typeThreshold, RoutineOverrides overrides)
    {
        if (overrides.DueSoonPercent is not { } percent)
        {
            return typeThreshold;
        }

        return DueSoonThreshold.Create(percent);
    }

    private static ParameterSource SourceOf<T>(T? overrideValue)
        where T : struct
    {
        if (overrideValue.HasValue)
        {
            return ParameterSource.Override;
        }

        return ParameterSource.RoutineType;
    }
}
