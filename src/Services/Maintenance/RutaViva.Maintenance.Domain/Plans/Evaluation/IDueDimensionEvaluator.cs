using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public interface IDueDimensionEvaluator
{
    DueDimension Dimension { get; }

    DimensionAssessment? Assess(MaintenanceCycle cycle, EffectiveRoutineParameters parameters, EvaluationContext context);
}
