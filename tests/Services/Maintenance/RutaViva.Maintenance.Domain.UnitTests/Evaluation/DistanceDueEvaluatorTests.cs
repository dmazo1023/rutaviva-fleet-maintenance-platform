using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Evaluation;

public sealed class DistanceDueEvaluatorTests
{
    private const long CycleStartKm = 50000;
    private const int IntervalKm = 10000;
    private const int IntervalMonths = 6;
    private const decimal ThresholdPercent = 10m;
    private const long OdometerAtLimit = 10000;
    private const long OdometerBeyondLimit = 12000;
    private const long OdometerAtStart = 0;
    private const decimal ExpectedPercentBeyondLimit = 120m;
    private const decimal ExpectedPercentAtStart = 0m;
    private const long ExpectedRemainingBeyondLimit = -2000;
    private const long ExpectedRemainingAtStart = 10000;

    private static readonly DateOnly CycleStartDate = new(2026, 8, 1);
    private static readonly DistanceDueEvaluator Evaluator = new();

    [Theory]
    [InlineData(8999, RoutineDueStatus.UpToDate)]
    [InlineData(9000, RoutineDueStatus.DueSoon)]
    [InlineData(OdometerAtLimit, RoutineDueStatus.Overdue)]
    public void Assess_Should_ClassifyStatus_When_OdometerReachesBoundary(long consumedKm, RoutineDueStatus expected)
    {
        DimensionAssessment? assessment = Evaluator.Assess(Cycle(), Parameters(IntervalKm, IntervalMonths), Context(CycleStartKm + consumedKm));

        assessment.Should().NotBeNull();
        assessment!.Status.Should().Be(expected);
        assessment.Dimension.Should().Be(DueDimension.Distance);
    }

    [Fact]
    public void Assess_Should_AllowConsumptionOverOneHundredPercent_When_OdometerExceedsInterval()
    {
        DimensionAssessment assessment = Evaluator.Assess(Cycle(), Parameters(IntervalKm, null), Context(CycleStartKm + OdometerBeyondLimit))!;

        assessment.Status.Should().Be(RoutineDueStatus.Overdue);
        assessment.ConsumedPercent.Should().Be(ExpectedPercentBeyondLimit);
        assessment.Remaining.Should().Be(ExpectedRemainingBeyondLimit);
    }

    [Fact]
    public void Assess_Should_ReportZeroConsumption_When_OdometerEqualsCycleStart()
    {
        DimensionAssessment assessment = Evaluator.Assess(Cycle(), Parameters(IntervalKm, null), Context(CycleStartKm + OdometerAtStart))!;

        assessment.Status.Should().Be(RoutineDueStatus.UpToDate);
        assessment.ConsumedPercent.Should().Be(ExpectedPercentAtStart);
        assessment.Remaining.Should().Be(ExpectedRemainingAtStart);
        assessment.DueSoonWindowPercent.Should().Be(ThresholdPercent);
    }

    [Fact]
    public void Assess_Should_ReturnNull_When_IntervalHasNoKilometers()
    {
        DimensionAssessment? assessment = Evaluator.Assess(Cycle(), Parameters(null, IntervalMonths), Context(CycleStartKm));

        assessment.Should().BeNull();
    }

    [Fact]
    public void Dimension_Should_BeDistance_When_Read()
    {
        Evaluator.Dimension.Should().Be(DueDimension.Distance);
    }

    private static MaintenanceCycle Cycle() => MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;

    private static EvaluationContext Context(long odometerKm) => EvaluationContext.Create(odometerKm, CycleStartDate).Value;

    private static EffectiveRoutineParameters Parameters(int? kilometers, int? months) =>
        EffectiveRoutineParameters.Resolve(
            MaintenanceInterval.Create(kilometers, months).Value,
            DueSoonThreshold.Create(ThresholdPercent).Value,
            RoutineOverrides.None).Value;
}
