using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class MaintenanceIntervalTests
{
    private const string EmptyCode = "MaintenanceInterval.Empty";
    private const string KilometersCode = "MaintenanceInterval.KilometersNotPositive";
    private const string MonthsCode = "MaintenanceInterval.MonthsNotPositive";
    private const string MonthsTooLargeCode = "MaintenanceInterval.MonthsTooLarge";
    private const int TenThousandKm = 10000;
    private const int SixMonths = 6;
    private const int OverMaxMonths = MaintenanceInterval.MaxMonths + 1;

    [Fact]
    public void Create_Should_ReturnInterval_When_BothDimensionsArePresent()
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(TenThousandKm, SixMonths);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kilometers.Should().Be(TenThousandKm);
        result.Value.Months.Should().Be(SixMonths);
        result.Value.HasDistance.Should().BeTrue();
        result.Value.HasTime.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_ReturnInterval_When_OnlyKilometersArePresent()
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(TenThousandKm, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDistance.Should().BeTrue();
        result.Value.HasTime.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_ReturnInterval_When_OnlyMonthsArePresent()
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(null, SixMonths);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDistance.Should().BeFalse();
        result.Value.HasTime.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_ReturnEmptyError_When_NoDimensionIsPresent()
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(EmptyCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_ReturnKilometersError_When_KilometersAreNotPositive(int kilometers)
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(kilometers, SixMonths);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(KilometersCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_ReturnMonthsError_When_MonthsAreNotPositive(int months)
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(TenThousandKm, months);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MonthsCode);
    }

    [Fact]
    public void Create_Should_ReturnMonthsTooLargeError_When_MonthsExceedMaximum()
    {
        Result<MaintenanceInterval> result = MaintenanceInterval.Create(null, OverMaxMonths);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MonthsTooLargeCode);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_BothDimensionsAreEqual()
    {
        MaintenanceInterval first = MaintenanceInterval.Create(TenThousandKm, SixMonths).Value;
        MaintenanceInterval second = MaintenanceInterval.Create(TenThousandKm, SixMonths).Value;

        first.Should().Be(second);
    }
}
