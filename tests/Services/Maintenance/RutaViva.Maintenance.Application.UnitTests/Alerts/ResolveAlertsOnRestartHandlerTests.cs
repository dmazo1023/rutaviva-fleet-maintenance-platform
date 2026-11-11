using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class ResolveAlertsOnRestartHandlerTests
{
    private const string AcknowledgedBy = "operator";
    private const long ServicedKm = 15000;
    private const int ServiceYear = 2026;
    private const int ServiceMonth = 3;
    private const int ServiceDay = 9;

    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly ResolveAlertsOnRestartHandler _handler;

    public ResolveAlertsOnRestartHandlerTests() => _handler = new ResolveAlertsOnRestartHandler(_alerts);

    [Fact]
    public async Task HandleAsync_Should_ResolveAllOpenAlertsWithRoutineRestarted_When_RoutineRestarts()
    {
        RoutineStatusSnapshot snapshot = AlertTestFactory.Snapshot();
        Alert active = AlertTestFactory.RaiseAlert(snapshot, AlertType.DueSoon);
        Alert acknowledged = AlertTestFactory.RaiseAlert(snapshot, AlertType.Overdue);
        acknowledged.Acknowledge(AcknowledgedBy, AlertTestFactory.OccurredAt());
        AlertTestFactory.StubOpenAlerts(_alerts, snapshot.RoutineId, null, active, acknowledged);
        RoutineRestarted domainEvent = new(snapshot, Guid.NewGuid(), ServiceRecord.Create(ServicedKm, new DateOnly(ServiceYear, ServiceMonth, ServiceDay)).Value, AlertTestFactory.OccurredAt());

        await _handler.HandleAsync(domainEvent, TestContext.Current.CancellationToken);

        active.ResolutionReason.Should().Be(AlertResolutionReason.RoutineRestarted);
        acknowledged.ResolutionReason.Should().Be(AlertResolutionReason.RoutineRestarted);
        acknowledged.Status.Should().Be(AlertStatus.Resolved);
        acknowledged.AcknowledgedBy.Should().Be(AcknowledgedBy);
    }
}
