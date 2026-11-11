using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.UnitTests.Workshops;

public sealed class ListWorkshopsQueryValidatorTests
{
    private const int UndefinedStatus = 0;
    private const char Filler = 'x';

    private readonly ListWorkshopsQueryValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_FiltersAreMissing()
    {
        bool isValid = _validator.Validate(new ListWorkshopsQuery(null, null)).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_FiltersAreValid()
    {
        bool isValid = _validator.Validate(new ListWorkshopsQuery(WorkshopStatus.Active, WorkshopTestFactory.CityValue)).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StatusIsUndefined()
    {
        bool isValid = _validator.Validate(new ListWorkshopsQuery((WorkshopStatus)UndefinedStatus, null)).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_CityIsTooLong()
    {
        string tooLong = new(Filler, PostalAddress.MaxCityLength + 1);

        bool isValid = _validator.Validate(new ListWorkshopsQuery(null, tooLong)).IsValid;

        isValid.Should().BeFalse();
    }
}
