using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public sealed class DistanceDueEvaluator : IDueDimensionEvaluator
{
    public DueDimension Dimension => DueDimension.Distance;

    public DimensionAssessment? Assess(MaintenanceCycle cycle, EffectiveRoutineParameters parameters, EvaluationContext context)
    {
        if (parameters.Interval.Kilometers is not { } intervalKm)
        {
            return null;
        }

        return Build(cycle, parameters.DueSoonThreshold, context, intervalKm);
    }

    private DimensionAssessment Build(MaintenanceCycle cycle, DueSoonThreshold threshold, EvaluationContext context, int intervalKm)
    {
        long consumedKm = context.OdometerKm - cycle.StartKm;
        long remaining = intervalKm - consumedKm;
        decimal window = threshold.Of(intervalKm);
        decimal consumedPercent = consumedKm * DueStatusClassifier.FullPercent / intervalKm;

        return new DimensionAssessment(
            Dimension,
            DueStatusClassifier.Classify(remaining, window),
            consumedPercent,
            remaining,
            threshold.Percent);
    }
}
