using FluentValidation;

namespace RutaViva.Maintenance.Application.Plans;

public sealed class ChangePlanStatusCommandValidator : AbstractValidator<ChangePlanStatusCommand>
{
    private const string TargetStatusInvalidMessage = "The target plan status is not valid.";

    public ChangePlanStatusCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsInEnum().WithMessage(TargetStatusInvalidMessage);
    }
}
