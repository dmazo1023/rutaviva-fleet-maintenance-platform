using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Evaluation;

public sealed class TimeDueEvaluatorTests
{
    private const long CycleStartKm = 1000;
    private const int IntervalKm = 10000;
    private const int SixMonths = 6;
    private const int TwelveMonths = 12;
    private const decimal ThresholdPercent = 10m;
    private const int DaysInSixMonthSpan = 184;
    private const int DaysInTwelveMonthSpan = 365;
    private const int DaysAfterDueDate = 30;
    private const decimal FullPercent = 100m;

    private static readonly DateOnly SixMonthStart = new(2026, 7, 1);
    private static readonly DateOnly SixMonthDue = new(2027, 1, 1);
    private static readonly DateOnly TwelveMonthStart = new(2026, 3, 1);
    private static readonly DateOnly TwelveMonthDue = new(2027, 3, 1);
    private static readonly TimeDueEvaluator Evaluator = new();

    [Theory]
    [InlineData(19, RoutineDueStatus.UpToDate)]
    [InlineData(18, RoutineDueStatus.DueSoon)]
    [InlineData(13, RoutineDueStatus.DueSoon)]
    [InlineData(1, RoutineDueStatus.DueSoon)]
    [InlineData(0, RoutineDueStatus.Overdue)]
    public void Assess_Should_UseFlooredWindowOf18Days_When_IntervalSpans184Days(int remainingDays, RoutineDueStatus expected)
    {
        DateOnly effectiveDate = SixMonthDue.AddDays(-remainingDays);

        DimensionAssessment assessment = Evaluator.Assess(Cycle(SixMonthStart), Parameters(SixMonths), Context(effectiveDate))!;

        (SixMonthDue.DayNumber - SixMonthStart.DayNumber).Should().Be(DaysInSixMonthSpan);
        assessment.Status.Should().Be(expected);
        assessment.Remaining.Should().Be(remainingDays);
        assessment.Dimension.Should().Be(DueDimension.Time);
    }

    [Theory]
    [InlineData(37, RoutineDueStatus.UpToDate)]
    [InlineData(36, RoutineDueStatus.DueSoon)]
    [InlineData(33, RoutineDueStatus.DueSoon)]
    [InlineData(0, RoutineDueStatus.Overdue)]
    public void Assess_Should_UseFlooredWindowOf36Days_When_IntervalSpans365Days(int remainingDays, RoutineDueStatus expected)
    {
        DateOnly effectiveDate = TwelveMonthDue.AddDays(-remainingDays);

        DimensionAssessment assessment = Evaluator.Assess(Cycle(TwelveMonthStart), Parameters(TwelveMonths), Context(effectiveDate))!;

        (TwelveMonthDue.DayNumber - TwelveMonthStart.DayNumber).Should().Be(DaysInTwelveMonthSpan);
        assessment.Status.Should().Be(expected);
    }

    [Fact]
    public void Assess_Should_BeOverdue_When_EffectiveDateEqualsDueDate()
    {
        DimensionAssessment assessment = Evaluator.Assess(Cycle(SixMonthStart), Parameters(SixMonths), Context(SixMonthDue))!;

        assessment.Status.Should().Be(RoutineDueStatus.Overdue);
        assessment.ConsumedPercent.Should().Be(FullPercent);
        assessment.DueSoonWindowPercent.Should().Be(ThresholdPercent);
    }

    [Fact]
    public void Assess_Should_AllowConsumptionOverOneHundredPercent_When_EffectiveDateIsAfterDueDate()
    {
        DimensionAssessment assessment = Evaluator.Assess(Cycle(SixMonthStart), Parameters(SixMonths), Context(SixMonthDue.AddDays(DaysAfterDueDate)))!;

        assessment.Status.Should().Be(RoutineDueStatus.Overdue);
        assessment.ConsumedPercent.Should().BeGreaterThan(FullPercent);
        assessment.Remaining.Should().Be(-DaysAfterDueDate);
    }

    [Fact]
    public void Assess_Should_ReturnNull_When_IntervalHasNoMonths()
    {
        EffectiveRoutineParameters distanceOnly = EffectiveRoutineParameters.Resolve(
            MaintenanceInterval.Create(IntervalKm, null).Value,
            DueSoonThreshold.Create(ThresholdPercent).Value,
            RoutineOverrides.None).Value;

        DimensionAssessment? assessment = Evaluator.Assess(Cycle(SixMonthStart), distanceOnly, Context(SixMonthStart));

        assessment.Should().BeNull();
    }

    [Fact]
    public void Dimension_Should_BeTime_When_Read()
    {
        Evaluator.Dimension.Should().Be(DueDimension.Time);
    }

    private static MaintenanceCycle Cycle(DateOnly startDate) => MaintenanceCycle.Start(CycleStartKm, startDate).Value;

    private static EvaluationContext Context(DateOnly effectiveDate) => EvaluationContext.Create(CycleStartKm, effectiveDate).Value;

    private static EffectiveRoutineParameters Parameters(int months) =>
        EffectiveRoutineParameters.Resolve(
            MaintenanceInterval.Create(null, months).Value,
            DueSoonThreshold.Create(ThresholdPercent).Value,
            RoutineOverrides.None).Value;
}
