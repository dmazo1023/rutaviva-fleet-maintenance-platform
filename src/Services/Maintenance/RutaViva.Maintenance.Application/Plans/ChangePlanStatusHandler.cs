using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Plans;

public sealed class ChangePlanStatusHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ChangePlanStatusCommand, VehicleId>
{
    public async Task<Result<VehicleId>> HandleAsync(ChangePlanStatusCommand command, CancellationToken cancellationToken)
    {
        MaintenancePlan? plan = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        await ApplyStatusAsync(plan, command.TargetStatus, cancellationToken);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.VehicleId;
    }

    private async Task ApplyStatusAsync(MaintenancePlan plan, MaintenancePlanStatus target, CancellationToken cancellationToken)
    {
        if (target == MaintenancePlanStatus.Suspended)
        {
            plan.Suspend();
            return;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        plan.Resume(clock.TodayIn(MaintenanceTimeZone.Bogota), catalog);
    }
}
