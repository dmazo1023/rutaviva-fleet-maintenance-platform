using FluentValidation;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Routines;

public sealed class ChangeRoutineStateCommandValidator : AbstractValidator<ChangeRoutineStateCommand>
{
    public ChangeRoutineStateCommandValidator()
    {
        RuleFor(command => command.UpdatedBy)
            .NotEmpty().WithMaintenanceError(MaintenanceErrors.UpdatedByRequired);
    }
}
