using RutaViva.Customers.Application.Customers;

namespace RutaViva.Customers.Application.UnitTests.Customers;

public sealed class RegisterCustomerCommandValidatorTests
{
    private const string EmptyText = "";
    private const string InvalidEmail = "not-an-email";
    private const string InvalidTaxId = "abc";
    private const string InvalidPhone = "12";
    private const int OverlongNameLength = 201;
    private const char FillCharacter = 'a';

    private readonly RegisterCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        bool isValid = _validator.Validate(Command()).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_PhoneIsNotProvided()
    {
        RegisterCustomerCommand command = Command() with { Phone = null };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_NameIsEmpty()
    {
        RegisterCustomerCommand command = Command() with { Name = EmptyText };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_NameIsTooLong()
    {
        RegisterCustomerCommand command = Command() with { Name = new string(FillCharacter, OverlongNameLength) };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailHasInvalidFormat()
    {
        RegisterCustomerCommand command = Command() with { Email = InvalidEmail };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_TaxIdHasInvalidFormat()
    {
        RegisterCustomerCommand command = Command() with { TaxId = InvalidTaxId };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_PhoneHasInvalidFormat()
    {
        RegisterCustomerCommand command = Command() with { Phone = InvalidPhone };

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    private static RegisterCustomerCommand Command() =>
        new(CustomerTestFactory.CustomerName, CustomerTestFactory.TaxIdText, CustomerTestFactory.CustomerEmail, CustomerTestFactory.PhoneText);
}
