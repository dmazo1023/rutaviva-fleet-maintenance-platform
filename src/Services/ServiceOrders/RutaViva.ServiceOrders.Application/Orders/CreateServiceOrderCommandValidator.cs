using FluentValidation;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class CreateServiceOrderCommandValidator : AbstractValidator<CreateServiceOrderCommand>
{
    private const string ReferenceRequiredMessage = "The service order reference is required.";

    public CreateServiceOrderCommandValidator()
    {
        RuleFor(command => command.Reference).NotNull().WithMessage(ReferenceRequiredMessage);
    }
}
