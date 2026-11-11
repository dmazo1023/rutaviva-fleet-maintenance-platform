namespace RutaViva.Maintenance.Domain.Alerts;

public enum AlertResolutionReason
{
    RoutineRestarted = 1,
    EscalatedToOverdue = 2,
    ParametersChanged = 3,
    RoutineDeactivated = 4
}
