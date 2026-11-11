using RutaViva.Maintenance.Domain.Plans.Evaluation;

namespace RutaViva.Maintenance.Domain.Plans;

public sealed record ReevaluationOutcome(int NewDueSoon, int NewOverdue)
{
    private const int CountStep = 1;

    public static readonly ReevaluationOutcome None = new(0, 0);

    internal ReevaluationOutcome Count(RoutineTransitionType type) => type switch
    {
        RoutineTransitionType.BecameDueSoon => this with { NewDueSoon = NewDueSoon + CountStep },
        RoutineTransitionType.BecameOverdue => this with { NewOverdue = NewOverdue + CountStep },
        _ => this
    };
}
