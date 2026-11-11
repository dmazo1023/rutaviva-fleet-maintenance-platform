using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Evaluation;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Evaluation;

public sealed class DueStatusPolicyTests
{
    private const decimal DistancePercent = 90m;
    private const decimal TimePercent = 120m;
    private const long AnyRemaining = 100;
    private const decimal AnyWindowPercent = 10m;
    private const long CycleStartKm = 1000;
    private const int IntervalKm = 10000;
    private const int IntervalMonths = 6;

    private static readonly DateOnly CycleStartDate = new(2026, 8, 1);

    [Theory]
    [InlineData(RoutineDueStatus.Overdue, RoutineDueStatus.Overdue, RoutineDueStatus.Overdue, AlertTrigger.DistanceAndTime)]
    [InlineData(RoutineDueStatus.Overdue, RoutineDueStatus.DueSoon, RoutineDueStatus.Overdue, AlertTrigger.Distance)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.Overdue, RoutineDueStatus.Overdue, AlertTrigger.Time)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.DueSoon, RoutineDueStatus.DueSoon, AlertTrigger.DistanceAndTime)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.UpToDate, RoutineDueStatus.DueSoon, AlertTrigger.Distance)]
    [InlineData(RoutineDueStatus.UpToDate, RoutineDueStatus.DueSoon, RoutineDueStatus.DueSoon, AlertTrigger.Time)]
    [InlineData(RoutineDueStatus.UpToDate, RoutineDueStatus.UpToDate, RoutineDueStatus.UpToDate, AlertTrigger.DistanceAndTime)]
    public void Combine_Should_ReturnWorstStatusAndTrigger_When_BothDimensionsPresent(
        RoutineDueStatus distance,
        RoutineDueStatus time,
        RoutineDueStatus expectedStatus,
        AlertTrigger expectedTrigger)
    {
        DimensionAssessment[] assessments = [Assessment(DueDimension.Distance, distance, DistancePercent), Assessment(DueDimension.Time, time, TimePercent)];

        RoutineAssessment result = DueStatusPolicy.Combine(assessments);

        result.Status.Should().Be(expectedStatus);
        result.Trigger.Should().Be(expectedTrigger);
    }

    [Theory]
    [InlineData(DueDimension.Distance, AlertTrigger.Distance)]
    [InlineData(DueDimension.Time, AlertTrigger.Time)]
    public void Combine_Should_UseSingleDimensionAsTrigger_When_OnlyOneDimensionIsPresent(DueDimension dimension, AlertTrigger expectedTrigger)
    {
        DimensionAssessment[] assessments = [Assessment(dimension, RoutineDueStatus.Overdue, TimePercent)];

        RoutineAssessment result = DueStatusPolicy.Combine(assessments);

        result.Status.Should().Be(RoutineDueStatus.Overdue);
        result.Trigger.Should().Be(expectedTrigger);
        result.ConsumedPercent.Should().Be(TimePercent);
    }

    [Fact]
    public void Combine_Should_ReturnMaximumConsumedPercent_When_DimensionsDiffer()
    {
        DimensionAssessment[] assessments =
        [
            Assessment(DueDimension.Distance, RoutineDueStatus.DueSoon, DistancePercent),
            Assessment(DueDimension.Time, RoutineDueStatus.Overdue, TimePercent)
        ];

        RoutineAssessment result = DueStatusPolicy.Combine(assessments);

        result.ConsumedPercent.Should().Be(TimePercent);
    }

    [Fact]
    public void Combine_Should_ReturnMaximumConsumedPercent_When_DistanceIsHigher()
    {
        DimensionAssessment[] assessments =
        [
            Assessment(DueDimension.Distance, RoutineDueStatus.Overdue, TimePercent),
            Assessment(DueDimension.Time, RoutineDueStatus.DueSoon, DistancePercent)
        ];

        RoutineAssessment result = DueStatusPolicy.Combine(assessments);

        result.ConsumedPercent.Should().Be(TimePercent);
    }

    [Fact]
    public void Combine_Should_KeepNextDue_When_ProvidedByCaller()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKm, CycleStartDate).Value;
        NextDue nextDue = NextDue.Calculate(cycle, MaintenanceInterval.Create(IntervalKm, IntervalMonths).Value);
        DimensionAssessment[] assessments = [Assessment(DueDimension.Distance, RoutineDueStatus.UpToDate, DistancePercent)];

        RoutineAssessment result = DueStatusPolicy.Combine(assessments, nextDue);

        result.NextDue.Should().Be(nextDue);
    }

    [Fact]
    public void Combine_Should_ReturnUpToDate_When_NoDimensionIsPresent()
    {
        RoutineAssessment result = DueStatusPolicy.Combine([]);

        result.Status.Should().Be(RoutineDueStatus.UpToDate);
        result.ConsumedPercent.Should().Be(0m);
    }

    private static DimensionAssessment Assessment(DueDimension dimension, RoutineDueStatus status, decimal consumedPercent) =>
        new(dimension, status, consumedPercent, AnyRemaining, AnyWindowPercent);
}
