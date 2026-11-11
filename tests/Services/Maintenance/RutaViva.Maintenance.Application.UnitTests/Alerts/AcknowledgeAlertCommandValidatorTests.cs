using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class AcknowledgeAlertCommandValidatorTests
{
    private const string AcknowledgedBy = "operator";

    private readonly AcknowledgeAlertCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_AcknowledgedByIsBlank(string acknowledgedBy)
    {
        AcknowledgeAlertCommand command = new(AlertId.New(), acknowledgedBy);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Pass_When_AcknowledgedByIsPresent()
    {
        AcknowledgeAlertCommand command = new(AlertId.New(), AcknowledgedBy);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeTrue();
    }
}
