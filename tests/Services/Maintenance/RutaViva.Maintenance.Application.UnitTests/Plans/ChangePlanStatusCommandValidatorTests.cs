using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.Plans;

public sealed class ChangePlanStatusCommandValidatorTests
{
    private const int UndefinedStatus = 0;

    private readonly ChangePlanStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(MaintenancePlanStatus.Active)]
    [InlineData(MaintenancePlanStatus.Suspended)]
    public void Validate_Should_Pass_When_StatusIsDefined(MaintenancePlanStatus status)
    {
        ChangePlanStatusCommand command = new(VehicleId.New(), status);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StatusIsUndefined()
    {
        ChangePlanStatusCommand command = new(VehicleId.New(), (MaintenancePlanStatus)UndefinedStatus);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
