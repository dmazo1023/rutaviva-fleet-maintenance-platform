using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

internal static class OutboundEventFactory
{
    private const string PlateValue = "ABC123";
    private const string TypeCodeValue = "OIL_CHANGE";
    private const string RoutineNameValue = "Oil change";
    private const int CycleNumberValue = 3;
    private const long CycleStartKmValue = 10000;
    private const int IntervalKmValue = 5000;
    private const int IntervalMonthsValue = 6;
    private const decimal DueSoonPercentValue = 20m;
    private const long OdometerKmValue = 14000;
    private const decimal ConsumedPercentValue = 80m;
    private const int StartYear = 2026;
    private const int StartMonth = 1;
    private const int StartDay = 15;
    private const int OccurredHour = 9;
    private const int ServiceKilometers = 15000;

    public static DateOnly StartDate() => new(StartYear, StartMonth, StartDay);

    public static DateTimeOffset OccurredAt() => new(StartYear, StartMonth, StartDay, OccurredHour, 0, 0, TimeSpan.Zero);

    public static LicensePlate Plate() => LicensePlate.Create(PlateValue).Value;

    public static ServiceRecord Service() => ServiceRecord.Create(ServiceKilometers, StartDate()).Value;

    public static RoutineStatusSnapshot Snapshot() =>
        new(
            RoutineId.New(),
            VehicleId.New(),
            Plate(),
            CustomerId.New(),
            RoutineTypeCode.Create(TypeCodeValue).Value,
            RoutineNameValue,
            RoutineState.Active,
            RoutineDueStatus.DueSoon,
            CycleNumberValue,
            CycleStartKmValue,
            StartDate(),
            IntervalKmValue,
            IntervalMonthsValue,
            DueSoonPercentValue,
            NextDueValue(),
            OdometerKmValue,
            ConsumedPercentValue);

    private static NextDue NextDueValue()
    {
        MaintenanceCycle cycle = MaintenanceCycle.Start(CycleStartKmValue, StartDate()).Value;
        MaintenanceInterval interval = MaintenanceInterval.Create(IntervalKmValue, IntervalMonthsValue).Value;
        return NextDue.Calculate(cycle, interval);
    }
}
