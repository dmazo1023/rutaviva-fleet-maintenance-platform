using FluentValidation;

namespace RutaViva.Customers.Application.Approvers;

public sealed class ChangeApproverStatusCommandValidator : AbstractValidator<ChangeApproverStatusCommand>
{
    private const string TargetStatusInvalidMessage = "The target approver status is not valid.";

    public ChangeApproverStatusCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsInEnum().WithMessage(TargetStatusInvalidMessage);
    }
}
