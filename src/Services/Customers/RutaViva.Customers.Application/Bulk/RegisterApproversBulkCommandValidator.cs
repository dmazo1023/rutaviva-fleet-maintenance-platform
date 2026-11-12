using FluentValidation;

namespace RutaViva.Customers.Application.Bulk;

public sealed class RegisterApproversBulkCommandValidator : AbstractValidator<RegisterApproversBulkCommand>
{
    public RegisterApproversBulkCommandValidator()
    {
        RuleFor(command => command.Rows.Count)
            .LessThanOrEqualTo(BulkRowLimits.MaxRows)
            .WithMessage(BulkRowLimits.TooManyRowsMessage);
    }
}
