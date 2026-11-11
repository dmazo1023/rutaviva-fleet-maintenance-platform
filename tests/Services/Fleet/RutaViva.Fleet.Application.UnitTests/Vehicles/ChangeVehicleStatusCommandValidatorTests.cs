using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class ChangeVehicleStatusCommandValidatorTests
{
    private const int UndefinedStatus = 0;

    private readonly ChangeVehicleStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(VehicleStatus.Active)]
    [InlineData(VehicleStatus.Inactive)]
    public void Validate_Should_Pass_When_StatusIsDefined(VehicleStatus status)
    {
        ChangeVehicleStatusCommand command = new(VehicleId.New(), status);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StatusIsUndefined()
    {
        ChangeVehicleStatusCommand command = new(VehicleId.New(), (VehicleStatus)UndefinedStatus);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_VehicleIdentifierIsEmpty()
    {
        ChangeVehicleStatusCommand command = new(default, VehicleStatus.Active);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
