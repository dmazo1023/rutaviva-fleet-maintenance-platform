using FluentValidation;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class ChangeVehicleStatusCommandValidator : AbstractValidator<ChangeVehicleStatusCommand>
{
    public ChangeVehicleStatusCommandValidator()
    {
        RuleFor(command => command.VehicleId).Must(id => id.Value != Guid.Empty).WithVehicleError(VehicleErrors.IdentifierEmpty);
        RuleFor(command => command.Target).IsInEnum().WithVehicleError(VehicleApplicationErrors.InvalidStatus);
    }
}
