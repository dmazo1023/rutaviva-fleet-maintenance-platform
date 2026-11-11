using FluentValidation;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed class UpdateRoutineTypeParametersCommandValidator : AbstractValidator<UpdateRoutineTypeParametersCommand>
{
    private const decimal MinimumDueSoonPercent = 1m;
    private const decimal MaximumDueSoonPercent = 99m;

    public UpdateRoutineTypeParametersCommandValidator()
    {
        RuleFor(command => command.UpdatedBy)
            .NotEmpty()
            .WithErrorCode(MaintenanceErrors.UpdatedByRequired.Code)
            .WithMessage(MaintenanceErrors.UpdatedByRequired.Message);
        RuleFor(command => command)
            .Must(HasAtLeastOneDimension)
            .WithErrorCode(MaintenanceErrors.MaintenanceIntervalEmpty.Code)
            .WithMessage(MaintenanceErrors.MaintenanceIntervalEmpty.Message);
        RuleFor(command => command.DueSoonPercent)
            .InclusiveBetween(MinimumDueSoonPercent, MaximumDueSoonPercent)
            .WithErrorCode(MaintenanceErrors.DueSoonThresholdOutOfRange.Code)
            .WithMessage(MaintenanceErrors.DueSoonThresholdOutOfRange.Message);
    }

    private static bool HasAtLeastOneDimension(UpdateRoutineTypeParametersCommand command) =>
        command.IntervalKm.HasValue || command.IntervalMonths.HasValue;
}
