using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.UnitTests.Vehicles.ValueObjects;

public sealed class VinTests
{
    private const string InvalidCode = "Vehicle.InvalidVin";
    private const string ValidVin = "9FB4SRC9AKM100101";
    private const string PaddedLowerVin = " 9fb4src9akm100101 ";
    private const string ElevenCharacters = "9GAJM52348B";
    private const string EighteenCharacters = "9FB4SRC9AKM1001011";
    private const string ContainsI = "9FB4SRC9AKM10010I";
    private const string ContainsO = "9FB4SRC9AKM10010O";
    private const string ContainsQ = "9FB4SRC9AKM10010Q";
    private const string ContainsLowerQ = "9fb4src9akm10010q";
    private const string ContainsSymbol = "9FB4SRC9AKM10010-";
    private const string Blank = "   ";

    [Fact]
    public void Create_Should_NormalizeToUpperCase_When_InputIsLowerWithPadding()
    {
        Result<Vin> result = Vin.Create(PaddedLowerVin);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(ValidVin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(ElevenCharacters)]
    [InlineData(EighteenCharacters)]
    [InlineData(ContainsI)]
    [InlineData(ContainsO)]
    [InlineData(ContainsQ)]
    [InlineData(ContainsLowerQ)]
    [InlineData(ContainsSymbol)]
    public void Create_Should_ReturnInvalidVinError_When_InputDoesNotMatchPattern(string? value)
    {
        Result<Vin> result = Vin.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
