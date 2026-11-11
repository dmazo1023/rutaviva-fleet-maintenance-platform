using FluentValidation;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class AssignResponsibleCommandValidator : AbstractValidator<AssignResponsibleCommand>
{
    public AssignResponsibleCommandValidator()
    {
        RuleFor(command => command.VehicleId).Must(id => id.Value != Guid.Empty).WithVehicleError(VehicleErrors.IdentifierEmpty);
        RuleFor(command => command.ResponsibleApproverId)
            .Must(id => id!.Value.Value != Guid.Empty)
            .WithVehicleError(VehicleErrors.IdentifierEmpty)
            .When(command => command.ResponsibleApproverId is not null);
    }
}
