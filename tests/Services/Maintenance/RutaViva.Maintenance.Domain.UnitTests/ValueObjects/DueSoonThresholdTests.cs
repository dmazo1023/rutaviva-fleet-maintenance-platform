using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class DueSoonThresholdTests
{
    private const string OutOfRangeCode = "DueSoonThreshold.OutOfRange";
    private const decimal TenPercent = 10m;
    private const decimal LowestAccepted = 0.01m;
    private const decimal HighestAccepted = 99.99m;
    private const decimal Zero = 0m;
    private const decimal OneHundred = 100m;
    private const decimal Negative = -5m;
    private const decimal AboveOneHundred = 100.5m;
    private const decimal IntervalKm = 10000m;
    private const decimal ExpectedWindow = 1000m;

    public static TheoryData<decimal> ValidPercents => new() { TenPercent, LowestAccepted, HighestAccepted };

    public static TheoryData<decimal> InvalidPercents => new() { Zero, OneHundred, Negative, AboveOneHundred };

    [Theory]
    [MemberData(nameof(ValidPercents))]
    public void Create_Should_ReturnThreshold_When_PercentIsBetweenZeroAndOneHundred(decimal percent)
    {
        Result<DueSoonThreshold> result = DueSoonThreshold.Create(percent);

        result.IsSuccess.Should().BeTrue();
        result.Value.Percent.Should().Be(percent);
    }

    [Theory]
    [MemberData(nameof(InvalidPercents))]
    public void Create_Should_ReturnOutOfRangeError_When_PercentIsOutsideOpenRange(decimal percent)
    {
        Result<DueSoonThreshold> result = DueSoonThreshold.Create(percent);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OutOfRangeCode);
    }

    [Fact]
    public void Of_Should_ReturnProportionalAmount_When_ThresholdIsTenPercent()
    {
        DueSoonThreshold threshold = DueSoonThreshold.Create(TenPercent).Value;

        threshold.Of(IntervalKm).Should().Be(ExpectedWindow);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_PercentIsEqual()
    {
        DueSoonThreshold first = DueSoonThreshold.Create(TenPercent).Value;
        DueSoonThreshold second = DueSoonThreshold.Create(TenPercent).Value;

        first.Should().Be(second);
    }
}
