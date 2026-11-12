using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal static class PlanProjector
{
    internal static MaintenancePlanDetailDto ToDetail(PlanReadRow plan, IEnumerable<RoutineReadRow> rows)
    {
        List<RoutineDetailDto> routines = ProjectRoutines(plan, rows);
        PlanStatistics statistics = PlanStatistics.From(routines);

        return new MaintenancePlanDetailDto(
            plan.VehicleId,
            plan.Plate.Value,
            plan.Status,
            plan.OdometerKm,
            statistics.WorstDueStatus,
            statistics.MaxConsumedPercent,
            statistics.UpToDateCount,
            statistics.DueSoonCount,
            statistics.OverdueCount,
            statistics.InactiveCount,
            routines);
    }

    internal static MaintenancePlanSummaryDto ToSummary(PlanReadRow plan, IEnumerable<RoutineReadRow> rows)
    {
        List<RoutineDetailDto> routines = ProjectRoutines(plan, rows);
        PlanStatistics statistics = PlanStatistics.From(routines);

        return new MaintenancePlanSummaryDto(
            plan.VehicleId,
            plan.Plate.Value,
            plan.Status,
            plan.OdometerKm,
            statistics.WorstDueStatus,
            statistics.MaxConsumedPercent,
            statistics.UpToDateCount,
            statistics.DueSoonCount,
            statistics.OverdueCount,
            statistics.InactiveCount,
            NextMaintenanceSelector.Select(routines));
    }

    private static List<RoutineDetailDto> ProjectRoutines(PlanReadRow plan, IEnumerable<RoutineReadRow> rows)
    {
        EvaluationContext context = EvaluationContext.Create(plan.OdometerKm, plan.LastTimeEvaluationDate).Value;

        return rows
            .OrderBy(row => row.Name, StringComparer.Ordinal)
            .Select(row => RoutineDetailProjector.Project(row, context))
            .ToList();
    }
}
