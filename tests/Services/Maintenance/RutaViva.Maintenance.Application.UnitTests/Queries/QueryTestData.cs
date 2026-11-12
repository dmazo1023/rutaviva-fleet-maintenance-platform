using System.Globalization;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

internal static class QueryTestData
{
    private const string Plate = "ABC123";
    private const string RoutineName = "Oil change";
    private const string RoutineTypeCode = "OIL_CHANGE";
    private const string Author = "system";
    private const string InstantText = "2026-11-12T08:00:00-05:00";
    private const long OdometerKilometers = 52300L;
    private const int CycleNumber = 1;
    private const int IntervalKilometers = 10000;
    private const int IntervalMonths = 6;
    private const decimal DueSoonPercent = 10m;
    private const decimal ConsumedPercent = 23m;
    private const int NoCount = 0;
    private const int SingleCount = 1;

    internal static DateTimeOffset Instant() => DateTimeOffset.Parse(InstantText, CultureInfo.InvariantCulture);

    internal static MaintenancePlanDetailDto PlanDetail(VehicleId vehicleId) =>
        new(
            vehicleId,
            Plate,
            MaintenancePlanStatus.Active,
            OdometerKilometers,
            RoutineDueStatus.UpToDate,
            ConsumedPercent,
            SingleCount,
            NoCount,
            NoCount,
            NoCount,
            []);

    internal static MaintenancePlanSummaryDto PlanSummary() =>
        new(
            VehicleId.New(),
            Plate,
            MaintenancePlanStatus.Active,
            OdometerKilometers,
            RoutineDueStatus.UpToDate,
            ConsumedPercent,
            SingleCount,
            NoCount,
            NoCount,
            NoCount,
            null);

    internal static OdometerReadingDto Reading() =>
        new(OdometerKilometers, Instant(), Instant(), OdometerReadingOutcome.Accepted, null);

    internal static RoutineTypeDto RoutineType() =>
        new(
            RoutineTypeId.New(),
            RoutineTypeCode,
            RoutineName,
            IntervalKilometers,
            IntervalMonths,
            DueSoonPercent,
            RoutineTypeStatus.Active,
            Author,
            Instant());

    internal static AlertDto Alert() =>
        new(
            AlertId.New(),
            VehicleId.New(),
            Plate,
            CustomerId.New(),
            RoutineId.New(),
            RoutineName,
            CycleNumber,
            AlertType.DueSoon,
            AlertTrigger.Distance,
            AlertStatus.Active,
            Instant(),
            null,
            null,
            null,
            null);
}
