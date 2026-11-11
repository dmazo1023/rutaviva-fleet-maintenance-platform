using FluentValidation.Results;
using RutaViva.Maintenance.Application.Evaluations;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Evaluations;

public sealed class ReevaluateMaintenancePlansCommandValidatorTests
{
    private readonly ReevaluateMaintenancePlansCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_BeValid_When_DateIsGiven()
    {
        ValidationResult result = _validator.Validate(new ReevaluateMaintenancePlansCommand(EvaluationTestData.AsOfDate, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_BeValid_When_VehicleIdIsGiven()
    {
        ValidationResult result = _validator.Validate(new ReevaluateMaintenancePlansCommand(EvaluationTestData.AsOfDate, VehicleId.New()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_DateIsDefault()
    {
        ValidationResult result = _validator.Validate(new ReevaluateMaintenancePlansCommand(default, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(ReevaluateMaintenancePlansCommand.AsOfDate));
    }
}
