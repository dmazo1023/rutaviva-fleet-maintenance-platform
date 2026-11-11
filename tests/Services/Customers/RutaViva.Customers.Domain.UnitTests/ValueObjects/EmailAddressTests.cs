using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.UnitTests.ValueObjects;

public sealed class EmailAddressTests
{
    private const string InvalidCode = "EmailAddress.Invalid";
    private const string TooLongCode = "EmailAddress.TooLong";
    private const string MixedCase = " Laura_Gomez@RutaViva.com ";
    private const string MixedCaseNormalized = "laura_gomez@rutaviva.com";
    private const string PlainEmail = "flota@rutaviva.example";
    private const string Blank = "   ";
    private const string MissingAtSign = "flota.rutaviva.example";
    private const string MissingDomainDot = "flota@rutaviva";
    private const string MissingLocalPart = "@rutaviva.example";
    private const string TwoAtSigns = "flota@@rutaviva.example";
    private const string InnerSpace = "flota flota@rutaviva.example";
    private const char LocalPartFiller = 'a';
    private const string DomainSuffix = "@rutaviva.example";

    [Theory]
    [InlineData(MixedCase, MixedCaseNormalized)]
    [InlineData(PlainEmail, PlainEmail)]
    public void Create_Should_ReturnLowercaseTrimmedValue_When_FormatIsValid(string input, string expected)
    {
        Result<EmailAddress> result = EmailAddress.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(MissingAtSign)]
    [InlineData(MissingDomainDot)]
    [InlineData(MissingLocalPart)]
    [InlineData(TwoAtSigns)]
    [InlineData(InnerSpace)]
    public void Create_Should_ReturnInvalidEmailError_When_FormatIsInvalid(string? input)
    {
        Result<EmailAddress> result = EmailAddress.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_Should_ReturnTooLongError_When_LengthExceedsMaximum()
    {
        string tooLong = string.Concat(new string(LocalPartFiller, EmailAddress.MaxLength), DomainSuffix);

        Result<EmailAddress> result = EmailAddress.Create(tooLong);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(TooLongCode);
    }

    [Fact]
    public void Create_Should_Succeed_When_LengthEqualsMaximum()
    {
        int localLength = EmailAddress.MaxLength - DomainSuffix.Length;
        string atLimit = string.Concat(new string(LocalPartFiller, localLength), DomainSuffix);

        Result<EmailAddress> result = EmailAddress.Create(atLimit);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeTrue_When_EmailsDifferOnlyInCase()
    {
        EmailAddress first = EmailAddress.Create(MixedCase).Value;
        EmailAddress second = EmailAddress.Create(MixedCaseNormalized).Value;

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
