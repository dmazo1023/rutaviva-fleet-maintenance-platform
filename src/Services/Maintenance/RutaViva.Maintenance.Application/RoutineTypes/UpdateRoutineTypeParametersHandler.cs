using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed class UpdateRoutineTypeParametersHandler(
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<UpdateRoutineTypeParametersCommand, RoutineTypeId>
{
    public async Task<Result<RoutineTypeId>> HandleAsync(UpdateRoutineTypeParametersCommand command, CancellationToken cancellationToken)
    {
        RoutineType? routineType = await routineTypes.GetAsync(command.RoutineTypeId, cancellationToken);
        if (routineType is null)
        {
            return MaintenanceErrors.RoutineTypeNotFound;
        }

        Result updated = UpdateParameters(routineType, command);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.RoutineTypeId;
    }

    private Result UpdateParameters(RoutineType routineType, UpdateRoutineTypeParametersCommand command)
    {
        Result<MaintenanceInterval> interval = MaintenanceInterval.Create(command.IntervalKm, command.IntervalMonths);
        if (interval.IsFailure)
        {
            return Result.Failure(interval.Error);
        }

        Result<DueSoonThreshold> threshold = DueSoonThreshold.Create(command.DueSoonPercent);
        if (threshold.IsFailure)
        {
            return Result.Failure(threshold.Error);
        }

        return routineType.UpdateParameters(interval.Value, threshold.Value, command.UpdatedBy, clock.UtcNow);
    }
}
