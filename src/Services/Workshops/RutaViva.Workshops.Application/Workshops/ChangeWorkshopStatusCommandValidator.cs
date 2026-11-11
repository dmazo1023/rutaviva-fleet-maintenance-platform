using FluentValidation;

namespace RutaViva.Workshops.Application.Workshops;

public sealed class ChangeWorkshopStatusCommandValidator : AbstractValidator<ChangeWorkshopStatusCommand>
{
    private const string TargetStatusInvalidMessage = "The target workshop status is not valid.";

    public ChangeWorkshopStatusCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsInEnum().WithMessage(TargetStatusInvalidMessage);
    }
}
