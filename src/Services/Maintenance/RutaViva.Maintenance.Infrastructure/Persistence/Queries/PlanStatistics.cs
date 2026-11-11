using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal sealed record PlanStatistics(
    RoutineDueStatus? WorstDueStatus,
    decimal MaxConsumedPercent,
    int UpToDateCount,
    int DueSoonCount,
    int OverdueCount,
    int InactiveCount)
{
    private const decimal NoConsumedPercent = 0m;

    private static readonly IReadOnlyList<RoutineDueStatus> SeverityOrder =
    [
        RoutineDueStatus.Overdue,
        RoutineDueStatus.DueSoon,
        RoutineDueStatus.UpToDate
    ];

    internal static PlanStatistics From(IReadOnlyList<RoutineDetailDto> routines)
    {
        List<RoutineDetailDto> active = routines.Where(routine => routine.State == RoutineState.Active).ToList();

        return new PlanStatistics(
            WorstOf(active),
            MaxConsumedOf(active),
            CountWith(active, RoutineDueStatus.UpToDate),
            CountWith(active, RoutineDueStatus.DueSoon),
            CountWith(active, RoutineDueStatus.Overdue),
            routines.Count - active.Count);
    }

    private static RoutineDueStatus? WorstOf(IReadOnlyList<RoutineDetailDto> active) =>
        SeverityOrder
            .Select(status => (RoutineDueStatus?)status)
            .FirstOrDefault(status => active.Any(routine => routine.DueStatus == status));

    private static decimal MaxConsumedOf(IReadOnlyList<RoutineDetailDto> active) =>
        active.Select(routine => routine.ConsumedPercent).DefaultIfEmpty(NoConsumedPercent).Max();

    private static int CountWith(IReadOnlyList<RoutineDetailDto> active, RoutineDueStatus status) =>
        active.Count(routine => routine.DueStatus == status);
}
