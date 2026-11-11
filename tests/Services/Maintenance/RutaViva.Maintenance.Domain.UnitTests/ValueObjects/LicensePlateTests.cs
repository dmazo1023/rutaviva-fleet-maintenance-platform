using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.ValueObjects;

public sealed class LicensePlateTests
{
    private const string InvalidCode = "LicensePlate.Invalid";
    private const string LowerWithSpaces = " rva 101 ";
    private const string Normalized = "RVA101";
    private const string MotorcyclePlate = "eva30d";
    private const string MotorcycleNormalized = "EVA30D";
    private const string TooShort = "RV101";
    private const string TooLong = "RVA1012";
    private const string StartsWithDigit = "1VA101";
    private const string SymbolAtEnd = "RVA10!";
    private const string LetterInDigitSlot = "RVAA01";
    private const string Blank = "   ";

    [Fact]
    public void Create_Should_NormalizeToUpperWithoutSpaces_When_InputIsLowerWithSpaces()
    {
        Result<LicensePlate> result = LicensePlate.Create(LowerWithSpaces);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Normalized);
    }

    [Fact]
    public void Create_Should_AcceptLetterAsLastCharacter_When_PlateBelongsToMotorcycle()
    {
        Result<LicensePlate> result = LicensePlate.Create(MotorcyclePlate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(MotorcycleNormalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(TooShort)]
    [InlineData(TooLong)]
    [InlineData(StartsWithDigit)]
    [InlineData(SymbolAtEnd)]
    [InlineData(LetterInDigitSlot)]
    public void Create_Should_ReturnInvalidPlateError_When_InputDoesNotMatchPattern(string? value)
    {
        Result<LicensePlate> result = LicensePlate.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_PlatesNormalizeToTheSameValue()
    {
        LicensePlate first = LicensePlate.Create(LowerWithSpaces).Value;
        LicensePlate second = LicensePlate.Create(Normalized).Value;

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void ToString_Should_ReturnNormalizedValue_When_PlateIsValid()
    {
        LicensePlate plate = LicensePlate.Create(LowerWithSpaces).Value;

        plate.ToString().Should().Be(Normalized);
    }
}
