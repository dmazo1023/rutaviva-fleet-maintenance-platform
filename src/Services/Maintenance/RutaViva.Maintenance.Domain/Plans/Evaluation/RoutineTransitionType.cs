namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public enum RoutineTransitionType
{
    None = 1,
    BecameDueSoon = 2,
    BecameOverdue = 3,
    RevertedToUpToDate = 4
}
