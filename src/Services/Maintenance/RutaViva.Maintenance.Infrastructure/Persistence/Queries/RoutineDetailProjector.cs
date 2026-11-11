using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal static class RoutineDetailProjector
{
    private static readonly IReadOnlyList<IDueDimensionEvaluator> Evaluators =
    [
        new DistanceDueEvaluator(),
        new TimeDueEvaluator()
    ];

    internal static RoutineDetailDto Project(RoutineReadRow row, EvaluationContext context)
    {
        MaintenanceCycle cycle = MaintenanceCycle.Create(row.CycleNumber, row.CycleStartKm, row.CycleStartDate).Value;
        EffectiveRoutineParameters parameters = ResolveParameters(row);
        List<DimensionAssessment> dimensions = AssessDimensions(cycle, parameters, context);
        NextDue nextDue = NextDue.Calculate(cycle, parameters.Interval);
        RoutineAssessment assessment = DueStatusPolicy.Combine(dimensions, nextDue);

        return Build(row, parameters, nextDue, assessment, dimensions);
    }

    private static EffectiveRoutineParameters ResolveParameters(RoutineReadRow row)
    {
        MaintenanceInterval typeInterval = MaintenanceInterval.Create(row.TypeIntervalKm, row.TypeIntervalMonths).Value;
        RoutineOverrides overrides = RoutineOverrides.Create(
            row.OverrideIntervalKm,
            row.OverrideIntervalMonths,
            row.OverrideDueSoonPercent).Value;

        return EffectiveRoutineParameters.Resolve(typeInterval, row.TypeThreshold, overrides).Value;
    }

    private static List<DimensionAssessment> AssessDimensions(
        MaintenanceCycle cycle,
        EffectiveRoutineParameters parameters,
        EvaluationContext context) =>
        Evaluators
            .Select(evaluator => evaluator.Assess(cycle, parameters, context))
            .OfType<DimensionAssessment>()
            .ToList();

    private static RoutineTrackDto? TrackOf(IReadOnlyList<DimensionAssessment> dimensions, DueDimension dimension, string kind)
    {
        DimensionAssessment? assessment = dimensions.FirstOrDefault(candidate => candidate.Dimension == dimension);
        if (assessment is null)
        {
            return null;
        }

        return new RoutineTrackDto(
            kind,
            assessment.ConsumedPercent,
            assessment.DueSoonWindowPercent,
            assessment.Remaining,
            assessment.Status);
    }

    private static RoutineDetailDto Build(
        RoutineReadRow row,
        EffectiveRoutineParameters parameters,
        NextDue nextDue,
        RoutineAssessment assessment,
        IReadOnlyList<DimensionAssessment> dimensions) =>
        new(
            row.RoutineId,
            row.TypeCode.Value,
            row.Name,
            row.State,
            row.DueStatus,
            row.TriggeredBy,
            row.CycleNumber,
            row.CycleStartKm,
            row.CycleStartDate,
            parameters.Interval.Kilometers,
            parameters.IntervalKilometersSource,
            parameters.Interval.Months,
            parameters.IntervalMonthsSource,
            parameters.DueSoonThreshold.Percent,
            parameters.DueSoonSource,
            nextDue.Kilometers,
            nextDue.Date,
            assessment.ConsumedPercent,
            TrackOf(dimensions, DueDimension.Distance, RoutineTrackDto.DistanceKind),
            TrackOf(dimensions, DueDimension.Time, RoutineTrackDto.TimeKind));
}
