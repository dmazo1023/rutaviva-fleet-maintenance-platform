using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class RaiseDueSoonAlertHandlerTests
{
    private const bool FirstNotice = true;
    private const bool RepeatedNotice = false;

    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly RaiseDueSoonAlertHandler _handler;

    public RaiseDueSoonAlertHandlerTests() => _handler = new RaiseDueSoonAlertHandler(_alerts);

    [Fact]
    public async Task HandleAsync_Should_NotRaiseAlert_When_NoticeIsNotFirstInCycle()
    {
        RoutineBecameDueSoon domainEvent = new(AlertTestFactory.Snapshot(), AlertTrigger.Distance, RepeatedNotice, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        _alerts.DidNotReceive().Add(Arg.Any<Alert>());
    }

    [Fact]
    public async Task HandleAsync_Should_RaiseDueSoonAlert_When_NoticeIsFirstInCycle()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot();
        RoutineBecameDueSoon domainEvent = new(snapshot, AlertTrigger.Time, FirstNotice, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        _alerts.Received(1).Add(Arg.Is<Alert>(alert => IsDueSoonAlertOf(alert, snapshot)));
    }

    private static bool IsDueSoonAlertOf(Alert alert, RoutineStatusSnapshot snapshot) =>
        alert.Type == AlertType.DueSoon
        && alert.Trigger == AlertTrigger.Time
        && alert.Status == AlertStatus.Active
        && alert.RoutineId == snapshot.RoutineId
        && alert.VehicleId == snapshot.VehicleId
        && alert.Plate == snapshot.Plate
        && alert.CustomerId == snapshot.CustomerId
        && alert.RoutineName == snapshot.RoutineName
        && alert.CycleNumber == snapshot.CycleNumber
        && alert.RaisedAt == AlertTestFactory.OccurredAt();
}
