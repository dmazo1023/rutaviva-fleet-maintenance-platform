using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.UnitTests.Workshops;

public sealed class ChangeWorkshopStatusCommandValidatorTests
{
    private const int UndefinedStatus = 0;

    private readonly ChangeWorkshopStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(WorkshopStatus.Active)]
    [InlineData(WorkshopStatus.Inactive)]
    public void Validate_Should_Pass_When_StatusIsDefined(WorkshopStatus status)
    {
        bool isValid = _validator.Validate(new ChangeWorkshopStatusCommand(WorkshopId.New(), status)).IsValid;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StatusIsUndefined()
    {
        ChangeWorkshopStatusCommand command = new(WorkshopId.New(), (WorkshopStatus)UndefinedStatus);

        bool isValid = _validator.Validate(command).IsValid;

        isValid.Should().BeFalse();
    }
}
