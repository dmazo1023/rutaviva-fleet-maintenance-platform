using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class EffectiveRoutineParametersTests
{
    private const int TypeKm = 3000;
    private const int TypeMonths = 6;
    private const decimal TypePercent = 10m;
    private const int OverrideKm = 5000;
    private const int OverrideMonths = 12;
    private const decimal OverridePercent = 20m;

    private static readonly MaintenanceInterval TypeInterval = MaintenanceInterval.Create(TypeKm, TypeMonths).Value;
    private static readonly DueSoonThreshold TypeThreshold = DueSoonThreshold.Create(TypePercent).Value;

    [Fact]
    public void Resolve_Should_UseTypeValuesAndTypeSource_When_NoOverridesExist()
    {
        Result<EffectiveRoutineParameters> result = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, RoutineOverrides.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Interval.Should().Be(TypeInterval);
        result.Value.DueSoonThreshold.Should().Be(TypeThreshold);
        result.Value.IntervalKilometersSource.Should().Be(ParameterSource.RoutineType);
        result.Value.IntervalMonthsSource.Should().Be(ParameterSource.RoutineType);
        result.Value.DueSoonSource.Should().Be(ParameterSource.RoutineType);
    }

    [Fact]
    public void Resolve_Should_OverrideOnlyPercent_When_OnlyPercentIsOverridden()
    {
        RoutineOverrides overrides = RoutineOverrides.Create(null, null, OverridePercent).Value;

        EffectiveRoutineParameters parameters = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, overrides).Value;

        parameters.DueSoonThreshold.Percent.Should().Be(OverridePercent);
        parameters.DueSoonSource.Should().Be(ParameterSource.Override);
        parameters.Interval.Should().Be(TypeInterval);
        parameters.IntervalKilometersSource.Should().Be(ParameterSource.RoutineType);
        parameters.IntervalMonthsSource.Should().Be(ParameterSource.RoutineType);
    }

    [Fact]
    public void Resolve_Should_OverrideIntervalDimensionsIndependently_When_OnlyKilometersAreOverridden()
    {
        RoutineOverrides overrides = RoutineOverrides.Create(OverrideKm, null, null).Value;

        EffectiveRoutineParameters parameters = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, overrides).Value;

        parameters.Interval.Kilometers.Should().Be(OverrideKm);
        parameters.Interval.Months.Should().Be(TypeMonths);
        parameters.IntervalKilometersSource.Should().Be(ParameterSource.Override);
        parameters.IntervalMonthsSource.Should().Be(ParameterSource.RoutineType);
    }

    [Fact]
    public void Resolve_Should_UseOverrideSourceForEveryValue_When_AllValuesAreOverridden()
    {
        RoutineOverrides overrides = RoutineOverrides.Create(OverrideKm, OverrideMonths, OverridePercent).Value;

        EffectiveRoutineParameters parameters = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, overrides).Value;

        parameters.Interval.Kilometers.Should().Be(OverrideKm);
        parameters.Interval.Months.Should().Be(OverrideMonths);
        parameters.DueSoonThreshold.Percent.Should().Be(OverridePercent);
        parameters.IntervalKilometersSource.Should().Be(ParameterSource.Override);
        parameters.IntervalMonthsSource.Should().Be(ParameterSource.Override);
        parameters.DueSoonSource.Should().Be(ParameterSource.Override);
    }

    [Fact]
    public void Resolve_Should_ReturnToTypeValues_When_OverridesAreRemoved()
    {
        EffectiveRoutineParameters withOverride = EffectiveRoutineParameters
            .Resolve(TypeInterval, TypeThreshold, RoutineOverrides.Create(null, null, OverridePercent).Value).Value;
        EffectiveRoutineParameters withoutOverride = EffectiveRoutineParameters
            .Resolve(TypeInterval, TypeThreshold, RoutineOverrides.None).Value;

        withoutOverride.DueSoonThreshold.Percent.Should().Be(TypePercent);
        withoutOverride.Should().NotBe(withOverride);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ResolvedFromSameInputs()
    {
        EffectiveRoutineParameters first = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, RoutineOverrides.None).Value;
        EffectiveRoutineParameters second = EffectiveRoutineParameters.Resolve(TypeInterval, TypeThreshold, RoutineOverrides.None).Value;

        first.Should().Be(second);
    }
}
