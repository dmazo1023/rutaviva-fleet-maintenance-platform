using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Routines;

public sealed class ChangeRoutineStateHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ChangeRoutineStateCommand, RoutineId>
{
    public async Task<Result<RoutineId>> HandleAsync(ChangeRoutineStateCommand command, CancellationToken cancellationToken)
    {
        MaintenancePlan? plan = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        DateOnly today = MaintenanceTimeZone.ToLocalDate(clock.UtcNow);
        Result changed = plan.ChangeRoutineState(command.RoutineId, command.State, command.UpdatedBy, catalog, today);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.RoutineId;
    }
}
