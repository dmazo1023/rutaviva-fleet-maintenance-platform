using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.UnitTests.ValueObjects;

public sealed class PhoneNumberTests
{
    private const string InvalidCode = "PhoneNumber.Invalid";
    private const string LandLine = "6045550001";
    private const string WithPrefix = "+576045550001";
    private const string WithSpaces = " +57 604 555 0001 ";
    private const string WithSpacesNormalized = "+576045550001";
    private const string Blank = "   ";
    private const string TooShort = "123456";
    private const string TooLong = "1234567890123456";
    private const string WithLetters = "604555ABCD";
    private const string WithHyphens = "604-555-0001";
    private const string PlusInTheMiddle = "60455+50001";
    private const string DoublePlus = "++576045550001";

    [Theory]
    [InlineData(LandLine, LandLine)]
    [InlineData(WithPrefix, WithPrefix)]
    [InlineData(WithSpaces, WithSpacesNormalized)]
    public void Create_Should_ReturnNormalizedValue_When_FormatIsValid(string input, string expected)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(input);

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
    [InlineData(WithHyphens)]
    [InlineData(PlusInTheMiddle)]
    [InlineData(DoublePlus)]
    public void Create_Should_ReturnInvalidPhoneError_When_FormatIsInvalid(string? input)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_ValuesNormalizeToTheSameText()
    {
        PhoneNumber first = PhoneNumber.Create(WithSpaces).Value;
        PhoneNumber second = PhoneNumber.Create(WithPrefix).Value;

        first.Should().Be(second);
    }
}
