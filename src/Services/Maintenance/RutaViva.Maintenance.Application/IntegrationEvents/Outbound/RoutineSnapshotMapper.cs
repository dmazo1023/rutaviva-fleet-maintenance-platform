using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

internal static class RoutineSnapshotMapper
{
    public static RoutineSnapshot Map(RoutineStatusSnapshot snapshot) =>
        new(
            snapshot.RoutineId.Value,
            snapshot.VehicleId.Value,
            snapshot.Plate.Value,
            snapshot.CustomerId.Value,
            snapshot.RoutineTypeCode.Value,
            snapshot.RoutineName,
            snapshot.State.ToString(),
            snapshot.DueStatus.ToString(),
            snapshot.CycleNumber,
            snapshot.CycleStartKm,
            snapshot.CycleStartDate,
            snapshot.IntervalKm,
            snapshot.IntervalMonths,
            RoundPercent(snapshot.EffectiveDueSoonPercent),
            snapshot.NextDue.Kilometers,
            snapshot.NextDue.Date,
            snapshot.OdometerKm,
            snapshot.ConsumedPercent);

    private static int RoundPercent(decimal percent) => (int)Math.Round(percent, MidpointRounding.AwayFromZero);
}
