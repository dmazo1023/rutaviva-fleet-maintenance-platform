namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public static class RoutineTransitionPolicy
{
    private static readonly IReadOnlyDictionary<(RoutineDueStatus Current, RoutineDueStatus Assessed), RoutineTransitionType> Table =
        new Dictionary<(RoutineDueStatus Current, RoutineDueStatus Assessed), RoutineTransitionType>
        {
            [(RoutineDueStatus.UpToDate, RoutineDueStatus.DueSoon)] = RoutineTransitionType.BecameDueSoon,
            [(RoutineDueStatus.UpToDate, RoutineDueStatus.Overdue)] = RoutineTransitionType.BecameOverdue,
            [(RoutineDueStatus.DueSoon, RoutineDueStatus.Overdue)] = RoutineTransitionType.BecameOverdue,
            [(RoutineDueStatus.DueSoon, RoutineDueStatus.UpToDate)] = RoutineTransitionType.RevertedToUpToDate
        };

    public static RoutineTransition Decide(RoutineDueStatus current, RoutineAssessment assessment)
    {
        if (!Table.TryGetValue((current, assessment.Status), out RoutineTransitionType type))
        {
            return new RoutineTransition(RoutineTransitionType.None, current, assessment.Trigger);
        }

        return new RoutineTransition(type, assessment.Status, assessment.Trigger);
    }
}
