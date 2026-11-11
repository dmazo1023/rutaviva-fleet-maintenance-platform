using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

internal static class AlertOriginFactory
{
    public static AlertOrigin From(RoutineStatusSnapshot snapshot) =>
        new(
            snapshot.VehicleId,
            snapshot.Plate,
            snapshot.CustomerId,
            snapshot.RoutineId,
            snapshot.RoutineName,
            snapshot.CycleNumber);
}
