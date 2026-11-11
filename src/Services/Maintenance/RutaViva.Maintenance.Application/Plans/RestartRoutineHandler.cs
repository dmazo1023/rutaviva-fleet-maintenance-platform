using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Plans;

public sealed class RestartRoutineHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork) : ICommandHandler<RestartRoutineCommand, RoutineId>
{
    public async Task<Result<RoutineId>> HandleAsync(RestartRoutineCommand command, CancellationToken cancellationToken)
    {
        MaintenancePlan? plan = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        Result<ServiceRecord> service = ServiceRecord.Create(command.ServicedKm, MaintenanceTimeZone.ToLocalDate(command.ServicedAt));
        if (service.IsFailure)
        {
            return service.Error;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        Result restarted = plan.RestartRoutine(command.RoutineId, service.Value, command.ServiceOrderId, catalog);
        if (restarted.IsFailure)
        {
            return restarted.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.RoutineId;
    }
}
