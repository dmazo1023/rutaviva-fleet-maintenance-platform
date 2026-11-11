using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.Events;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed class ReevaluatePlansOnRoutineTypeChangedHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IClock clock) : IDomainEventHandler<RoutineTypeParametersChanged>
{
    public async Task HandleAsync(RoutineTypeParametersChanged domainEvent, CancellationToken cancellationToken)
    {
        IReadOnlyList<VehicleId> planIds = await plans.GetActivePlanIdsWithRoutineTypeAsync(domainEvent.RoutineTypeId, cancellationToken);
        if (planIds.Count == 0)
        {
            return;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        DateOnly today = clock.TodayIn(MaintenanceTimeZone.Bogota);
        foreach (VehicleId planId in planIds)
        {
            await ReevaluateAsync(planId, catalog, today, cancellationToken);
        }
    }

    private async Task ReevaluateAsync(VehicleId planId, RoutineParameterCatalog catalog, DateOnly today, CancellationToken cancellationToken)
    {
        MaintenancePlan? plan = await plans.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return;
        }

        plan.Reevaluate(today, catalog);
    }
}
