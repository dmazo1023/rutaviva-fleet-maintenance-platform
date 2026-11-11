using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles.ValueObjects;

public sealed class KilometersTests
{
    private const string InvalidCode = "Vehicle.InvalidOdometer";
    private const long Zero = 0;
    private const long Typical = 52300;
    private const long NegativeFiveHundred = -500;
    private const long MinusOne = -1;

    [Theory]
    [InlineData(Zero)]
    [InlineData(Typical)]
    public void Create_Should_Succeed_When_ValueIsZeroOrPositive(long value)
    {
        Result<Kilometers> result = Kilometers.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(NegativeFiveHundred)]
    [InlineData(MinusOne)]
    public void Create_Should_ReturnInvalidOdometerError_When_ValueIsNegative(long value)
    {
        Result<Kilometers> result = Kilometers.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
