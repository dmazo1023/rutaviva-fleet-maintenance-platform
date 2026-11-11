using FluentValidation;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

public sealed class RegisterRoutinesBulkCommandValidator : AbstractValidator<RegisterRoutinesBulkCommand>
{
    public const int MaxRows = 500;

    private const string TooManyRowsCode = "Bulk.TooManyRows";
    private const string TooManyRowsMessage = "The batch exceeds the maximum number of rows.";

    public RegisterRoutinesBulkCommandValidator()
    {
        RuleFor(command => command.Rows.Count)
            .LessThanOrEqualTo(MaxRows)
            .WithErrorCode(TooManyRowsCode)
            .WithMessage(TooManyRowsMessage);
    }
}
