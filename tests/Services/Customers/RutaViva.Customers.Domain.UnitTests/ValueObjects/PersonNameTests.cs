using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.UnitTests.ValueObjects;

public sealed class PersonNameTests
{
    private const string RequiredCode = "PersonName.Required";
    private const string TooLongCode = "PersonName.TooLong";
    private const string PaddedName = "  Laura Gómez  ";
    private const string TrimmedName = "Laura Gómez";
    private const string CompanyName = "RutaViva S.A.S.";
    private const string Blank = "   ";
    private const char Filler = 'a';

    [Theory]
    [InlineData(PaddedName, TrimmedName)]
    [InlineData(CompanyName, CompanyName)]
    public void Create_Should_ReturnTrimmedValue_When_NameIsNotBlank(string input, string expected)
    {
        Result<PersonName> result = PersonName.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    public void Create_Should_ReturnRequiredError_When_NameIsBlank(string? input)
    {
        Result<PersonName> result = PersonName.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RequiredCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_Should_ReturnTooLongError_When_LengthExceedsMaximum()
    {
        string tooLong = new(Filler, PersonName.MaxLength + 1);

        Result<PersonName> result = PersonName.Create(tooLong);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(TooLongCode);
    }

    [Fact]
    public void Create_Should_Succeed_When_LengthEqualsMaximum()
    {
        string atLimit = new(Filler, PersonName.MaxLength);

        Result<PersonName> result = PersonName.Create(atLimit);

        result.IsSuccess.Should().BeTrue();
    }
}
