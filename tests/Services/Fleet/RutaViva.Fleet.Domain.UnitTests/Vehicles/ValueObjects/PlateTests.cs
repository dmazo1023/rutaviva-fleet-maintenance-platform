using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles.ValueObjects;

public sealed class PlateTests
{
    private const string InvalidCode = "Vehicle.InvalidPlate";
    private const string PaddedLower = " eva30d ";
    private const string MotorcycleNormalized = "EVA30D";
    private const string CarPlate = "RVA101";
    private const string WithHyphen = "RV-107";
    private const string TooShort = "RV101";
    private const string TooLong = "RVA1012";
    private const string StartsWithDigit = "1VA101";
    private const string SymbolAtEnd = "RVA10!";
    private const string LetterInDigitSlot = "RVAA01";
    private const string InnerSpace = "RVA 101";
    private const string Blank = "   ";

    [Fact]
    public void Create_Should_NormalizeToUpperCase_When_InputIsLowerWithPadding()
    {
        Result<Plate> result = Plate.Create(PaddedLower);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(MotorcycleNormalized);
    }

    [Fact]
    public void Create_Should_Succeed_When_PlateBelongsToCar()
    {
        Result<Plate> result = Plate.Create(CarPlate);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be(CarPlate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(WithHyphen)]
    [InlineData(TooShort)]
    [InlineData(TooLong)]
    [InlineData(StartsWithDigit)]
    [InlineData(SymbolAtEnd)]
    [InlineData(LetterInDigitSlot)]
    [InlineData(InnerSpace)]
    public void Create_Should_ReturnInvalidPlateError_When_InputDoesNotMatchPattern(string? value)
    {
        Result<Plate> result = Plate.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_PlatesNormalizeToTheSameValue()
    {
        Plate first = Plate.Create(PaddedLower).Value;
        Plate second = Plate.Create(MotorcycleNormalized).Value;

        first.Should().Be(second);
    }
}
