using FluentValidation;

namespace RutaViva.Customers.Application.Customers;

public sealed class ChangeCustomerStatusCommandValidator : AbstractValidator<ChangeCustomerStatusCommand>
{
    private const string TargetStatusInvalidMessage = "The target customer status is not valid.";

    public ChangeCustomerStatusCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsInEnum().WithMessage(TargetStatusInvalidMessage);
    }
}
