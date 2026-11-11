using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class ResolveAlertsOnRevertHandlerTests
{
    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly ResolveAlertsOnRevertHandler _handler;

    public ResolveAlertsOnRevertHandlerTests() => _handler = new ResolveAlertsOnRevertHandler(_alerts);

    [Fact]
    public async Task HandleAsync_Should_ResolveOpenDueSoonWithParametersChanged_When_StatusReverts()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot();
        Alert dueSoon = AlertTestFactory.RaiseAlert(snapshot, AlertType.DueSoon);
        AlertTestFactory.StubOpenAlerts(_alerts, snapshot.RoutineId, AlertType.DueSoon, dueSoon);
        RoutineDueStatusReverted domainEvent = new(snapshot, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        dueSoon.Status.Should().Be(AlertStatus.Resolved);
        dueSoon.ResolutionReason.Should().Be(AlertResolutionReason.ParametersChanged);
    }
}
