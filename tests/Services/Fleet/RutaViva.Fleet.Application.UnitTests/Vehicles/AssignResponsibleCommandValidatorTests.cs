using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class AssignResponsibleCommandValidatorTests
{
    private readonly AssignResponsibleCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_ResponsibleIsProvided()
    {
        AssignResponsibleCommand command = new(VehicleId.New(), ApproverId.New());

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_ResponsibleIsRemoved()
    {
        AssignResponsibleCommand command = new(VehicleId.New(), null);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_ResponsibleIdentifierIsEmpty()
    {
        AssignResponsibleCommand command = new(VehicleId.New(), default(ApproverId));

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_VehicleIdentifierIsEmpty()
    {
        AssignResponsibleCommand command = new(default, null);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
