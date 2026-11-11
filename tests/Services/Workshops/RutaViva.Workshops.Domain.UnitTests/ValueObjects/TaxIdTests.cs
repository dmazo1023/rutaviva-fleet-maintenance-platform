using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.UnitTests.ValueObjects;

public sealed class TaxIdTests
{
    private const string InvalidCode = "TaxId.Invalid";
    private const string NineDigits = "900100001";
    private const string TenDigits = "9001000011";
    private const string PaddedNineDigits = "  900100001  ";
    private const string EightDigits = "90010000";
    private const string ElevenDigits = "90010000112";
    private const string WithHyphen = "900100001-1";
    private const string WithLetter = "90010000A";
    private const string WithDots = "900.100.001";
    private const string Blank = "   ";

    [Theory]
    [InlineData(NineDigits)]
    [InlineData(TenDigits)]
    [InlineData(PaddedNineDigits)]
    public void Create_Should_ReturnTaxId_When_ValueHasValidFormat(string value)
    {
        Result<TaxId> result = TaxId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(EightDigits)]
    [InlineData(ElevenDigits)]
    [InlineData(WithHyphen)]
    [InlineData(WithLetter)]
    [InlineData(WithDots)]
    public void Create_Should_ReturnInvalidError_When_ValueHasInvalidFormat(string? value)
    {
        Result<TaxId> result = TaxId.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ValuesAreTheSameAfterTrim()
    {
        TaxId first = TaxId.Create(PaddedNineDigits).Value;
        TaxId second = TaxId.Create(NineDigits).Value;

        first.Should().Be(second);
    }
}
