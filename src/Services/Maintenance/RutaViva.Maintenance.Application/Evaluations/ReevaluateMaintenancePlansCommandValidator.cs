using FluentValidation;

namespace RutaViva.Maintenance.Application.Evaluations;

public sealed class ReevaluateMaintenancePlansCommandValidator : AbstractValidator<ReevaluateMaintenancePlansCommand>
{
    private const string AsOfDateRequiredMessage = "The evaluation date is required.";

    public ReevaluateMaintenancePlansCommandValidator()
    {
        RuleFor(command => command.AsOfDate).NotEqual(default(DateOnly)).WithMessage(AsOfDateRequiredMessage);
    }
}
