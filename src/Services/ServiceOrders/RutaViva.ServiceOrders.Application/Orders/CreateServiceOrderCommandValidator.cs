using FluentValidation;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class CreateServiceOrderCommandValidator : AbstractValidator<CreateServiceOrderCommand>
{
    private const string VehicleRequiredMessage = "The vehicle identifier is required.";
    private const string RoutineRequiredMessage = "The routine identifier is required.";
    private const string OriginInvalidMessage = "The service order origin is not valid.";

    public CreateServiceOrderCommandValidator()
    {
        RuleFor(command => command.VehicleId).NotEmpty().WithMessage(VehicleRequiredMessage);
        RuleFor(command => command.RoutineId).NotEmpty().WithMessage(RoutineRequiredMessage);
        RuleFor(command => command.Origin).IsInEnum().WithMessage(OriginInvalidMessage);
    }
}
