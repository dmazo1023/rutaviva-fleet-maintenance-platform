using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public static class DueStatusPolicy
{
    private const decimal EmptyConsumedPercent = 0m;
    private const AlertTrigger EmptyTrigger = AlertTrigger.Distance;
    private const int UpToDateSeverity = 0;
    private const int DueSoonSeverity = 1;
    private const int OverdueSeverity = 2;

    private static readonly IReadOnlyDictionary<RoutineDueStatus, int> Severity = new Dictionary<RoutineDueStatus, int>
    {
        [RoutineDueStatus.UpToDate] = UpToDateSeverity,
        [RoutineDueStatus.DueSoon] = DueSoonSeverity,
        [RoutineDueStatus.Overdue] = OverdueSeverity
    };

    public static RoutineAssessment Combine(IReadOnlyList<DimensionAssessment> assessments, NextDue? nextDue = null)
    {
        if (assessments.Count == 0)
        {
            return new RoutineAssessment(RoutineDueStatus.UpToDate, EmptyTrigger, EmptyConsumedPercent, nextDue);
        }

        RoutineDueStatus status = MostSevere(assessments);
        AlertTrigger trigger = TriggerOf(assessments, status);
        decimal consumedPercent = assessments.Max(assessment => assessment.ConsumedPercent);

        return new RoutineAssessment(status, trigger, consumedPercent, nextDue);
    }

    private static RoutineDueStatus MostSevere(IReadOnlyList<DimensionAssessment> assessments) =>
        assessments.Select(assessment => assessment.Status).MaxBy(status => Severity[status]);

    private static AlertTrigger TriggerOf(IReadOnlyList<DimensionAssessment> assessments, RoutineDueStatus status)
    {
        bool distance = ReachesStatus(assessments, status, DueDimension.Distance);
        bool time = ReachesStatus(assessments, status, DueDimension.Time);

        if (distance && time)
        {
            return AlertTrigger.DistanceAndTime;
        }

        if (distance)
        {
            return AlertTrigger.Distance;
        }

        return AlertTrigger.Time;
    }

    private static bool ReachesStatus(IReadOnlyList<DimensionAssessment> assessments, RoutineDueStatus status, DueDimension dimension) =>
        assessments.Any(assessment => assessment.Status == status && assessment.Dimension == dimension);
}
