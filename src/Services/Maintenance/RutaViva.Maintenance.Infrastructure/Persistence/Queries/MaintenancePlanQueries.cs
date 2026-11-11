using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

public sealed class MaintenancePlanQueries(MaintenanceDbContext context) : IMaintenancePlanQueries
{
    public async Task<MaintenancePlanDetailDto?> GetDetailAsync(VehicleId id, CancellationToken cancellationToken)
    {
        IQueryable<MaintenancePlan> plans = context.MaintenancePlans.AsNoTracking().Where(plan => plan.Id == id);
        PlanReadRow? plan = await PlanRows(plans).SingleOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        List<RoutineReadRow> rows = await RoutineRows(plans).ToListAsync(cancellationToken);
        return PlanProjector.ToDetail(plan, rows);
    }

    public async Task<IReadOnlyList<MaintenancePlanSummaryDto>> GetSummariesAsync(CustomerId? customerId, CancellationToken cancellationToken)
    {
        IQueryable<MaintenancePlan> plans = FilterByCustomer(context.MaintenancePlans.AsNoTracking(), customerId);
        List<PlanReadRow> planRows = await PlanRows(plans).ToListAsync(cancellationToken);
        List<RoutineReadRow> routineRows = await RoutineRows(plans).ToListAsync(cancellationToken);
        ILookup<VehicleId, RoutineReadRow> routinesByPlan = routineRows.ToLookup(row => row.VehicleId);

        return planRows
            .OrderBy(plan => plan.Plate.Value, StringComparer.Ordinal)
            .Select(plan => PlanProjector.ToSummary(plan, routinesByPlan[plan.VehicleId]))
            .ToList();
    }

    private static IQueryable<MaintenancePlan> FilterByCustomer(IQueryable<MaintenancePlan> plans, CustomerId? customerId)
    {
        if (customerId is not { } customer)
        {
            return plans;
        }

        return plans.Where(plan => plan.CustomerId == customer);
    }

    private static IQueryable<PlanReadRow> PlanRows(IQueryable<MaintenancePlan> plans) =>
        plans.Select(plan => new PlanReadRow(
            plan.Id,
            plan.Plate,
            plan.Status,
            plan.Odometer.Kilometers,
            plan.LastTimeEvaluationDate));

    private IQueryable<RoutineReadRow> RoutineRows(IQueryable<MaintenancePlan> plans) =>
        from plan in plans
        from routine in plan.Routines
        join routineType in context.RoutineTypes.AsNoTracking() on routine.RoutineTypeId equals routineType.Id
        select new RoutineReadRow(
            plan.Id,
            routine.Id,
            routine.RoutineTypeCode,
            routine.Name,
            routine.State,
            routine.DueStatus,
            routine.LastTrigger,
            routine.Cycle.Number,
            routine.Cycle.StartKm,
            routine.Cycle.StartDate,
            routine.Overrides.IntervalKm,
            routine.Overrides.IntervalMonths,
            routine.Overrides.DueSoonPercent,
            routineType.DefaultInterval.Kilometers,
            routineType.DefaultInterval.Months,
            routineType.DueSoonThreshold);
}
