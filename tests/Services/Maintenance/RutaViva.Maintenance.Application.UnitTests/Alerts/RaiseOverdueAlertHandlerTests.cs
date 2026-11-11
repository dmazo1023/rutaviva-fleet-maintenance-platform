using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class RaiseOverdueAlertHandlerTests
{
    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly RaiseOverdueAlertHandler _handler;

    public RaiseOverdueAlertHandlerTests() => _handler = new RaiseOverdueAlertHandler(_alerts);

    [Fact]
    public async Task HandleAsync_Should_ResolveDueSoonBeforeRaisingOverdue_When_RoutineEscalates()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot();
        Alert dueSoon = AlertTestFactory.RaiseAlert(snapshot, AlertType.DueSoon);
        AlertTestFactory.StubOpenAlerts(_alerts, snapshot.RoutineId, AlertType.DueSoon, dueSoon);
        AlertStatus? dueSoonStatusWhenOverdueAdded = null;
        _alerts.When(repository => repository.Add(Arg.Any<Alert>())).Do(_ => dueSoonStatusWhenOverdueAdded = dueSoon.Status);
        RoutineBecameOverdue domainEvent = new(snapshot, AlertTrigger.Distance, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        dueSoonStatusWhenOverdueAdded.Should().Be(AlertStatus.Resolved);
        dueSoon.ResolutionReason.Should().Be(AlertResolutionReason.EscalatedToOverdue);
        dueSoon.ResolvedAt.Should().Be(AlertTestFactory.OccurredAt());
    }

    [Fact]
    public async Task HandleAsync_Should_RaiseActiveOverdueAlert_When_RoutineBecomesOverdue()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot();
        AlertTestFactory.StubOpenAlerts(_alerts, snapshot.RoutineId, AlertType.DueSoon);
        RoutineBecameOverdue domainEvent = new(snapshot, AlertTrigger.DistanceAndTime, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        _alerts.Received(1).Add(Arg.Is<Alert>(alert => IsOverdueAlertOf(alert, snapshot)));
    }

    private static bool IsOverdueAlertOf(Alert alert, RoutineStatusSnapshot snapshot) =>
        alert.Type == AlertType.Overdue
        && alert.Trigger == AlertTrigger.DistanceAndTime
        && alert.Status == AlertStatus.Active
        && alert.RoutineId == snapshot.RoutineId
        && alert.CycleNumber == snapshot.CycleNumber;
}
