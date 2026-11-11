using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Routines;

public sealed class UpdateRoutineParametersHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<UpdateRoutineParametersCommand, RoutineId>
{
    public async Task<Result<RoutineId>> HandleAsync(UpdateRoutineParametersCommand command, CancellationToken cancellationToken)
    {
        Result<RoutineOverrides> overrides = RoutineOverrides.Create(command.IntervalKm, command.IntervalMonths, command.DueSoonPercent);
        if (overrides.IsFailure)
        {
            return overrides.Error;
        }

        MaintenancePlan? plan = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        DateOnly today = MaintenanceTimeZone.ToLocalDate(clock.UtcNow);
        Result updated = plan.UpdateRoutineOverrides(command.RoutineId, overrides.Value, command.UpdatedBy, catalog, today);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.RoutineId;
    }
}
