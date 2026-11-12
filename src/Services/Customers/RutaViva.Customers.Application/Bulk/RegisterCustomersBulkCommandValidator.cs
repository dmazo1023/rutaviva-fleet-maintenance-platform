using FluentValidation;

namespace RutaViva.Customers.Application.Bulk;

public sealed class RegisterCustomersBulkCommandValidator : AbstractValidator<RegisterCustomersBulkCommand>
{
    public RegisterCustomersBulkCommandValidator()
    {
        RuleFor(command => command.Rows.Count)
            .LessThanOrEqualTo(BulkRowLimits.MaxRows)
            .WithMessage(BulkRowLimits.TooManyRowsMessage);
    }
}
