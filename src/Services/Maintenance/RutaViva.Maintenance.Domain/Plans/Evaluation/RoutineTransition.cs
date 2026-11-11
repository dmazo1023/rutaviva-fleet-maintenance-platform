using RutaViva.Maintenance.Domain.Alerts;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public sealed record RoutineTransition(RoutineTransitionType Type, RoutineDueStatus Status, AlertTrigger Trigger)
{
    public bool HasChanged => Type != RoutineTransitionType.None;
}
