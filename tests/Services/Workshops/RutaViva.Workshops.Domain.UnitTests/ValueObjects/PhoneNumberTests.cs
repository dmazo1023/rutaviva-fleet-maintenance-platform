using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.UnitTests.ValueObjects;

public sealed class PhoneNumberTests
{
    private const string InvalidCode = "PhoneNumber.Invalid";
    private const string Landline = "6044440001";
    private const string SevenDigits = "4440001";
    private const string International = "+576044440001";
    private const string FifteenDigits = "123456789012345";
    private const string PaddedLandline = " 6044440001 ";
    private const string SixDigits = "444000";
    private const string SixteenDigits = "1234567890123456";
    private const string WithHyphens = "604-444-0001";
    private const string WithLetter = "604444000A";
    private const string DoublePlus = "++576044440001";
    private const string PlusInTheMiddle = "60444+40001";
    private const string Blank = "   ";

    [Theory]
    [InlineData(Landline)]
    [InlineData(SevenDigits)]
    [InlineData(International)]
    [InlineData(FifteenDigits)]
    public void Create_Should_ReturnPhone_When_ValueHasValidFormat(string value)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_Should_TrimSpaces_When_InputIsPadded()
    {
        Result<PhoneNumber> result = PhoneNumber.Create(PaddedLandline);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Landline);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(SixDigits)]
    [InlineData(SixteenDigits)]
    [InlineData(WithHyphens)]
    [InlineData(WithLetter)]
    [InlineData(DoublePlus)]
    [InlineData(PlusInTheMiddle)]
    public void Create_Should_ReturnInvalidError_When_ValueHasInvalidFormat(string? value)
    {
        Result<PhoneNumber> result = PhoneNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
