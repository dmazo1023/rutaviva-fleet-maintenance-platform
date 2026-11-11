using FluentValidation;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Routines;

public sealed class UpdateRoutineParametersCommandValidator : AbstractValidator<UpdateRoutineParametersCommand>
{
    private const int MinimumInterval = 1;
    private const decimal MinimumPercent = 1m;
    private const decimal MaximumPercent = 99m;

    public UpdateRoutineParametersCommandValidator()
    {
        RuleFor(command => command.UpdatedBy)
            .NotEmpty().WithMaintenanceError(MaintenanceErrors.UpdatedByRequired);
        RuleFor(command => command.IntervalKm)
            .GreaterThanOrEqualTo(MinimumInterval).When(command => command.IntervalKm.HasValue)
            .WithMaintenanceError(MaintenanceErrors.MaintenanceIntervalKilometersNotPositive);
        RuleFor(command => command.IntervalMonths)
            .GreaterThanOrEqualTo(MinimumInterval).When(command => command.IntervalMonths.HasValue)
            .WithMaintenanceError(MaintenanceErrors.MaintenanceIntervalMonthsNotPositive);
        RuleFor(command => command.DueSoonPercent)
            .InclusiveBetween(MinimumPercent, MaximumPercent).When(command => command.DueSoonPercent.HasValue)
            .WithMaintenanceError(MaintenanceErrors.DueSoonThresholdOutOfRange);
    }
}
