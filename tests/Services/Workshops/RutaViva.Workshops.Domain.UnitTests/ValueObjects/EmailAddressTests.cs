using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.UnitTests.ValueObjects;

public sealed class EmailAddressTests
{
    private const string InvalidCode = "EmailAddress.Invalid";
    private const string ValidEmail = "contacto@autofrenos.example";
    private const string MixedCaseWithSpaces = "  Contacto@Autofrenos.Example ";
    private const string SubdomainEmail = "servicio@mail.servicentro80.example";
    private const string WithoutAt = "contacto.autofrenos.example";
    private const string WithoutDomainDot = "contacto@autofrenos";
    private const string WithoutLocalPart = "@autofrenos.example";
    private const string WithInnerSpace = "con tacto@autofrenos.example";
    private const string TwoAtSigns = "a@b@autofrenos.example";
    private const string Blank = "   ";
    private const int OversizedLength = EmailAddress.MaxLength + 1;
    private const string Domain = "@a.example";

    [Theory]
    [InlineData(ValidEmail)]
    [InlineData(SubdomainEmail)]
    public void Create_Should_ReturnEmail_When_ValueHasValidFormat(string value)
    {
        Result<EmailAddress> result = EmailAddress.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_Should_NormalizeToLowerWithoutSpaces_When_InputIsMixedCaseWithSpaces()
    {
        Result<EmailAddress> result = EmailAddress.Create(MixedCaseWithSpaces);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(ValidEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(Blank)]
    [InlineData(WithoutAt)]
    [InlineData(WithoutDomainDot)]
    [InlineData(WithoutLocalPart)]
    [InlineData(WithInnerSpace)]
    [InlineData(TwoAtSigns)]
    public void Create_Should_ReturnInvalidError_When_ValueHasInvalidFormat(string? value)
    {
        Result<EmailAddress> result = EmailAddress.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_Should_ReturnInvalidError_When_ValueExceedsMaxLength()
    {
        string oversized = new string('a', OversizedLength - Domain.Length) + Domain;

        Result<EmailAddress> result = EmailAddress.Create(oversized);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(InvalidCode);
    }
}
