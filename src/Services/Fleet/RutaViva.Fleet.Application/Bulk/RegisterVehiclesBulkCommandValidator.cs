using FluentValidation;

namespace RutaViva.Fleet.Application.Bulk;

public sealed class RegisterVehiclesBulkCommandValidator : AbstractValidator<RegisterVehiclesBulkCommand>
{
    public const int MaxRows = 500;

    private const string TooManyRowsCode = "Bulk.TooManyRows";
    private const string TooManyRowsMessage = "The batch exceeds the maximum number of rows.";

    public RegisterVehiclesBulkCommandValidator()
    {
        RuleFor(command => command.Rows.Count)
            .LessThanOrEqualTo(MaxRows)
            .WithErrorCode(TooManyRowsCode)
            .WithMessage(TooManyRowsMessage);
    }
}
