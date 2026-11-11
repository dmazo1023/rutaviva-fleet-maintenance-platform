using NSubstitute;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

internal static class AlertTestFactory
{
    private const int Year = 2026;
    private const int Month = 3;
    private const int Day = 10;
    private const int Hour = 10;

    public static DateTimeOffset OccurredAt() => new(Year, Month, Day, Hour, 0, 0, TimeSpan.Zero);

    public static RoutineStatusSnapshot Snapshot() => OutboundEventFactory.Snapshot();

    public static Alert RaiseAlert(RoutineStatusSnapshot snapshot, AlertType type)
    {
        AlertOrigin origin = new(
            snapshot.VehicleId,
            snapshot.Plate,
            snapshot.CustomerId,
            snapshot.RoutineId,
            snapshot.RoutineName,
            snapshot.CycleNumber);
        return Alert.Raise(origin, type, AlertTrigger.Distance, OccurredAt());
    }

    public static void StubOpenAlerts(IAlertRepository alerts, RoutineId routineId, AlertType? type, params Alert[] open)
    {
        IReadOnlyList<Alert> result = open;
        alerts.GetOpenByRoutineAsync(routineId, type, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
    }
}
