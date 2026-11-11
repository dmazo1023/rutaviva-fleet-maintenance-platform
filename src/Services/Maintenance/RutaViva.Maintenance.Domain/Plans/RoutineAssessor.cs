using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans;

internal static class RoutineAssessor
{
    private static readonly IReadOnlyList<IDueDimensionEvaluator> Evaluators =
    [
        new DistanceDueEvaluator(),
        new TimeDueEvaluator()
    ];

    internal static RoutineAssessment Assess(
        MaintenanceCycle cycle,
        EffectiveRoutineParameters parameters,
        EvaluationContext context)
    {
        List<DimensionAssessment> dimensions = Evaluators
            .Select(evaluator => evaluator.Assess(cycle, parameters, context))
            .OfType<DimensionAssessment>()
            .ToList();

        return DueStatusPolicy.Combine(dimensions, NextDue.Calculate(cycle, parameters.Interval));
    }
}
