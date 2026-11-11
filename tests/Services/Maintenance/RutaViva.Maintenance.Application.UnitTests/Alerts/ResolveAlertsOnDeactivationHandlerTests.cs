using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class ResolveAlertsOnDeactivationHandlerTests
{
    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly ResolveAlertsOnDeactivationHandler _handler;

    public ResolveAlertsOnDeactivationHandlerTests() => _handler = new ResolveAlertsOnDeactivationHandler(_alerts);

    [Fact]
    public async Task HandleAsync_Should_ResolveOpenAlertsWithRoutineDeactivated_When_RoutineBecomesInactive()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot() with { State = RoutineState.Inactive };
        Alert overdue = AlertTestFactory.RaiseAlert(snapshot, AlertType.Overdue);
        AlertTestFactory.StubOpenAlerts(_alerts, snapshot.RoutineId, null, overdue);
        RoutineStateChanged domainEvent = new(snapshot, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        overdue.Status.Should().Be(AlertStatus.Resolved);
        overdue.ResolutionReason.Should().Be(AlertResolutionReason.RoutineDeactivated);
    }

    [Fact]
    public async Task HandleAsync_Should_NotTouchAlerts_When_RoutineBecomesActive()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot() with { State = RoutineState.Active };
        RoutineStateChanged domainEvent = new(snapshot, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        await _alerts.DidNotReceive().GetOpenByRoutineAsync(Arg.Any<RoutineId>(), Arg.Any<AlertType?>(), Arg.Any<CancellationToken>());
    }
}
