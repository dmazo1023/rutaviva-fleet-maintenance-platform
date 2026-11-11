using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Evaluation;

namespace RutaViva.Maintenance.Domain.UnitTests.Evaluation;

public sealed class RoutineTransitionPolicyTests
{
    private const decimal AnyConsumedPercent = 100m;

    [Theory]
    [InlineData(RoutineDueStatus.UpToDate, RoutineDueStatus.DueSoon, RoutineTransitionType.BecameDueSoon, RoutineDueStatus.DueSoon)]
    [InlineData(RoutineDueStatus.UpToDate, RoutineDueStatus.Overdue, RoutineTransitionType.BecameOverdue, RoutineDueStatus.Overdue)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.Overdue, RoutineTransitionType.BecameOverdue, RoutineDueStatus.Overdue)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.UpToDate, RoutineTransitionType.RevertedToUpToDate, RoutineDueStatus.UpToDate)]
    [InlineData(RoutineDueStatus.Overdue, RoutineDueStatus.UpToDate, RoutineTransitionType.None, RoutineDueStatus.Overdue)]
    [InlineData(RoutineDueStatus.Overdue, RoutineDueStatus.DueSoon, RoutineTransitionType.None, RoutineDueStatus.Overdue)]
    [InlineData(RoutineDueStatus.Overdue, RoutineDueStatus.Overdue, RoutineTransitionType.None, RoutineDueStatus.Overdue)]
    [InlineData(RoutineDueStatus.UpToDate, RoutineDueStatus.UpToDate, RoutineTransitionType.None, RoutineDueStatus.UpToDate)]
    [InlineData(RoutineDueStatus.DueSoon, RoutineDueStatus.DueSoon, RoutineTransitionType.None, RoutineDueStatus.DueSoon)]
    public void Decide_Should_ApplyTransitionTable_When_StatusIsAssessed(
        RoutineDueStatus current,
        RoutineDueStatus assessed,
        RoutineTransitionType expectedType,
        RoutineDueStatus expectedStatus)
    {
        RoutineTransition transition = RoutineTransitionPolicy.Decide(current, Assessment(assessed, AlertTrigger.Distance));

        transition.Type.Should().Be(expectedType);
        transition.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void Decide_Should_JumpDirectlyToOverdue_When_CurrentIsUpToDate()
    {
        RoutineTransition transition = RoutineTransitionPolicy.Decide(RoutineDueStatus.UpToDate, Assessment(RoutineDueStatus.Overdue, AlertTrigger.Time));

        transition.Type.Should().Be(RoutineTransitionType.BecameOverdue);
        transition.HasChanged.Should().BeTrue();
    }

    [Fact]
    public void Decide_Should_CarryDistanceAndTimeTrigger_When_BothDimensionsAreOverdue()
    {
        RoutineTransition transition = RoutineTransitionPolicy.Decide(
            RoutineDueStatus.UpToDate,
            Assessment(RoutineDueStatus.Overdue, AlertTrigger.DistanceAndTime));

        transition.Trigger.Should().Be(AlertTrigger.DistanceAndTime);
    }

    [Fact]
    public void Decide_Should_NotRegress_When_CurrentIsOverdueAndAssessmentIsUpToDate()
    {
        RoutineTransition transition = RoutineTransitionPolicy.Decide(RoutineDueStatus.Overdue, Assessment(RoutineDueStatus.UpToDate, AlertTrigger.Distance));

        transition.HasChanged.Should().BeFalse();
        transition.Status.Should().Be(RoutineDueStatus.Overdue);
    }

    private static RoutineAssessment Assessment(RoutineDueStatus status, AlertTrigger trigger) =>
        new(status, trigger, AnyConsumedPercent, null);
}
