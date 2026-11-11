using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public sealed class TimeDueEvaluator : IDueDimensionEvaluator
{
    public DueDimension Dimension => DueDimension.Time;

    public DimensionAssessment? Assess(MaintenanceCycle cycle, EffectiveRoutineParameters parameters, EvaluationContext context)
    {
        if (parameters.Interval.Months is not { } intervalMonths)
        {
            return null;
        }

        return Build(cycle, parameters.DueSoonThreshold, context, intervalMonths);
    }

    private DimensionAssessment Build(MaintenanceCycle cycle, DueSoonThreshold threshold, EvaluationContext context, int intervalMonths)
    {
        DateOnly dueDate = cycle.StartDate.AddMonths(intervalMonths);
        int totalDays = dueDate.DayNumber - cycle.StartDate.DayNumber;
        int remaining = dueDate.DayNumber - context.EffectiveDate.DayNumber;
        decimal window = Math.Floor(threshold.Of(totalDays));
        decimal consumedPercent = (totalDays - remaining) * DueStatusClassifier.FullPercent / totalDays;

        return new DimensionAssessment(
            Dimension,
            DueStatusClassifier.Classify(remaining, window),
            consumedPercent,
            remaining,
            threshold.Percent);
    }
}
