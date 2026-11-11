using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.UnitTests.ValueObjects;

public sealed class TaxIdTests
{
    private const string InvalidCode = "TaxId.Invalid";
    private const string PlainNit = "900000001";
    private const string NitWithVerificationDigit = "900000001-7";
    private const string NitWithSpaces = " 900 000 001 - 7 ";
    private const string NitWithSpacesNormalized = "900000001-7";
    private const string Blank = "   ";
    private const string TooShort = "12345";
    private const string TooLong = "1234567890123";
    private const string WithLetters = "9000A0001";
    private const string WithDots = "900.000.001";
    private const string TwoVerificationDigits = "900000001-71";
    private const string MissingVerificationDigit = "900000001-";

    [Theory]
    [InlineData(PlainNit, PlainNit)]
    [InlineData(NitWithVerificationDigit, NitWithVerificationDigit)]
    [InlineData(NitWithSpaces, NitWithSpacesNormalized)]
    public void Create_Should_ReturnNormalizedValue_When_FormatIsValid(string input, string expected)
    {
        Result<TaxId> result = TaxId.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(TooShort)]
    [InlineData(TooLong)]
    [InlineData(WithLetters)]
    [InlineData(WithDots)]
    [InlineData(TwoVerificationDigits)]
    [InlineData(MissingVerificationDigit)]
    public void Create_Should_ReturnInvalidTaxIdError_When_FormatIsInvalid(string? input)
    {
        Result<TaxId> result = TaxId.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ValuesNormalizeToTheSameText()
    {
        TaxId first = TaxId.Create(NitWithSpaces).Value;
        TaxId second = TaxId.Create(NitWithSpacesNormalized).Value;

        first.Should().Be(second);
    }

    [Fact]
    public void ToString_Should_ReturnNormalizedValue_When_TaxIdIsValid()
    {
        TaxId taxId = TaxId.Create(NitWithSpaces).Value;

        taxId.ToString().Should().Be(NitWithSpacesNormalized);
    }
}
