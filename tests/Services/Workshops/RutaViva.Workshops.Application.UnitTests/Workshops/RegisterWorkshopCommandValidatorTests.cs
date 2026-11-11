using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.UnitTests.Workshops;

public sealed class RegisterWorkshopCommandValidatorTests
{
    private const string BlankValue = "   ";
    private const string InvalidTaxId = "12AB";
    private const string InvalidEmail = "not-an-email";
    private const string InvalidPhone = "12";
    private const char Filler = 'x';

    private readonly RegisterWorkshopCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        bool isValid = _validator.Validate(WorkshopTestFactory.Command()).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_PhoneIsMissing()
    {
        bool isValid = _validator.Validate(WorkshopTestFactory.Command(phone: null)).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_FailOnAddress_When_AddressIsBlank()
    {
        string[] failing = FailingProperties(WorkshopTestFactory.Command(address: BlankValue));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.Address));
    }

    [Fact]
    public void Validate_Should_FailOnName_When_NameIsTooLong()
    {
        string tooLong = new(Filler, Workshop.MaxNameLength + 1);

        string[] failing = FailingProperties(WorkshopTestFactory.Command(name: tooLong));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.Name));
    }

    [Fact]
    public void Validate_Should_FailOnCity_When_CityIsTooLong()
    {
        string tooLong = new(Filler, PostalAddress.MaxCityLength + 1);

        string[] failing = FailingProperties(WorkshopTestFactory.Command(city: tooLong));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.City));
    }

    [Fact]
    public void Validate_Should_FailOnTaxId_When_TaxIdIsInvalid()
    {
        string[] failing = FailingProperties(WorkshopTestFactory.Command(taxId: InvalidTaxId));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.TaxId));
    }

    [Fact]
    public void Validate_Should_FailOnEmail_When_EmailIsInvalid()
    {
        string[] failing = FailingProperties(WorkshopTestFactory.Command(email: InvalidEmail));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.Email));
    }

    [Fact]
    public void Validate_Should_FailOnPhone_When_PhoneIsInvalid()
    {
        string[] failing = FailingProperties(WorkshopTestFactory.Command(phone: InvalidPhone));

        failing.Should().Contain(nameof(RegisterWorkshopCommand.Phone));
    }

    private string[] FailingProperties(RegisterWorkshopCommand command) =>
        [.. _validator.Validate(command).Errors.Select(failure => failure.PropertyName)];
}
