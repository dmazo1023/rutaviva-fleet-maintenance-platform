using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class RoutineOverridesTests
{
    private const string KilometersCode = "MaintenanceInterval.KilometersNotPositive";
    private const string MonthsCode = "MaintenanceInterval.MonthsNotPositive";
    private const string PercentCode = "DueSoonThreshold.OutOfRange";
    private const int IntervalKm = 20000;
    private const int IntervalMonths = 12;
    private const decimal TwentyPercent = 20m;
    private const decimal OneHundred = 100m;
    private const int Zero = 0;

    [Fact]
    public void Create_Should_ReturnOverrides_When_AllValuesAreValid()
    {
        Result<RoutineOverrides> result = RoutineOverrides.Create(IntervalKm, IntervalMonths, TwentyPercent);

        result.IsSuccess.Should().BeTrue();
        result.Value.IntervalKm.Should().Be(IntervalKm);
        result.Value.IntervalMonths.Should().Be(IntervalMonths);
        result.Value.DueSoonPercent.Should().Be(TwentyPercent);
    }

    [Fact]
    public void Create_Should_ReturnOverrides_When_AllValuesAreNull()
    {
        Result<RoutineOverrides> result = RoutineOverrides.Create(null, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(RoutineOverrides.None);
    }

    [Fact]
    public void Create_Should_ReturnKilometersError_When_KilometersAreNotPositive()
    {
        Result<RoutineOverrides> result = RoutineOverrides.Create(Zero, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(KilometersCode);
    }

    [Fact]
    public void Create_Should_ReturnMonthsError_When_MonthsAreNotPositive()
    {
        Result<RoutineOverrides> result = RoutineOverrides.Create(null, Zero, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MonthsCode);
    }

    [Fact]
    public void Create_Should_ReturnPercentError_When_PercentIsOneHundred()
    {
        Result<RoutineOverrides> result = RoutineOverrides.Create(null, null, OneHundred);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(PercentCode);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ValuesAreEqual()
    {
        RoutineOverrides first = RoutineOverrides.Create(IntervalKm, null, TwentyPercent).Value;
        RoutineOverrides second = RoutineOverrides.Create(IntervalKm, null, TwentyPercent).Value;

        first.Should().Be(second);
    }
}
