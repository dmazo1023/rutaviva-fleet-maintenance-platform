using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class OdometerTests
{
    private const string NegativeCode = "Odometer.KilometersNegative";
    private const string ReadingNegativeCode = "OdometerReading.KilometersNegative";
    private const long CurrentKm = 52300;
    private const long HigherKm = 53000;
    private const long LowerKm = 52000;
    private const long NegativeKm = -1;
    private const long ZeroKm = 0;

    private static readonly DateTimeOffset OdometerReadAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReadingReadAt = new(2026, 11, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnOdometer_When_KilometersAreZero()
    {
        Result<Odometer> result = Odometer.Create(ZeroKm, OdometerReadAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kilometers.Should().Be(ZeroKm);
        result.Value.ReadAt.Should().Be(OdometerReadAt);
    }

    [Fact]
    public void Create_Should_ReturnNegativeKilometersError_When_KilometersAreNegative()
    {
        Result<Odometer> result = Odometer.Create(NegativeKm, OdometerReadAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NegativeCode);
    }

    [Fact]
    public void IsAdvancedBy_Should_BeTrue_When_ReadingIsGreater()
    {
        Odometer odometer = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        OdometerReading reading = OdometerReading.Create(HigherKm, ReadingReadAt).Value;

        odometer.IsAdvancedBy(reading).Should().BeTrue();
    }

    [Fact]
    public void IsAdvancedBy_Should_BeFalse_When_ReadingIsEqual()
    {
        Odometer odometer = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        OdometerReading reading = OdometerReading.Create(CurrentKm, ReadingReadAt).Value;

        odometer.IsAdvancedBy(reading).Should().BeFalse();
    }

    [Fact]
    public void IsAdvancedBy_Should_BeFalse_When_ReadingIsLower()
    {
        Odometer odometer = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        OdometerReading reading = OdometerReading.Create(LowerKm, ReadingReadAt).Value;

        odometer.IsAdvancedBy(reading).Should().BeFalse();
    }

    [Fact]
    public void Advance_Should_ReturnOdometerWithReadingValues_When_ReadingIsGreater()
    {
        Odometer odometer = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        OdometerReading reading = OdometerReading.Create(HigherKm, ReadingReadAt).Value;

        Odometer advanced = odometer.Advance(reading);

        advanced.Kilometers.Should().Be(HigherKm);
        advanced.ReadAt.Should().Be(ReadingReadAt);
        odometer.Kilometers.Should().Be(CurrentKm);
    }

    [Fact]
    public void Advance_Should_ReturnSameOdometer_When_ReadingIsNotGreater()
    {
        Odometer odometer = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        OdometerReading reading = OdometerReading.Create(LowerKm, ReadingReadAt).Value;

        Odometer advanced = odometer.Advance(reading);

        advanced.Should().BeSameAs(odometer);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_KilometersAndReadAtAreEqual()
    {
        Odometer first = Odometer.Create(CurrentKm, OdometerReadAt).Value;
        Odometer second = Odometer.Create(CurrentKm, OdometerReadAt).Value;

        first.Should().Be(second);
    }

    [Fact]
    public void ReadingCreate_Should_ReturnReading_When_KilometersAreZero()
    {
        Result<OdometerReading> result = OdometerReading.Create(ZeroKm, ReadingReadAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kilometers.Should().Be(ZeroKm);
        result.Value.ReadAt.Should().Be(ReadingReadAt);
    }

    [Fact]
    public void ReadingCreate_Should_ReturnNegativeKilometersError_When_KilometersAreNegative()
    {
        Result<OdometerReading> result = OdometerReading.Create(NegativeKm, ReadingReadAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ReadingNegativeCode);
    }
}
