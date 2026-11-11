using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.Plans;

internal static class PlanTestFactory
{
    public const string PlateValue = "ABC123";
    public const long OdometerKm = 50000;

    private const string OilChangeCode = "OIL_CHANGE";
    private const string OilChangeName = "Oil change";
    private const int OilChangeKm = 10000;
    private const int OilChangeMonths = 6;
    private const decimal DueSoonPercent = 10m;
    private const int Year = 2026;
    private const int Month = 3;
    private const int LateUtcDay = 11;
    private const int LateUtcHour = 2;

    public static readonly RoutineTypeId OilChangeId = RoutineTypeId.New();

    public static DateTimeOffset LateUtcInstant() => new(Year, Month, LateUtcDay, LateUtcHour, 0, 0, TimeSpan.Zero);

    public static DateOnly LocalDateOfLateUtcInstant() => MaintenanceTimeZone.ToLocalDate(LateUtcInstant());

    public static LicensePlate Plate() => LicensePlate.Create(PlateValue).Value;

    public static MaintenancePlan Plan(VehicleId vehicleId, MaintenancePlanStatus status) =>
        MaintenancePlan.Create(
            vehicleId,
            Plate(),
            CustomerId.New(),
            status,
            Odometer.Create(OdometerKm, LateUtcInstant()).Value,
            LocalDateOfLateUtcInstant());

    public static MaintenancePlan PlanWithRoutine(VehicleId vehicleId, out RoutineId routineId)
    {
        MaintenancePlan plan = Plan(vehicleId, MaintenancePlanStatus.Active);
        RoutineRegistration registration = RoutineRegistration.Create(OilChangeId, null, null, RoutineOverrides.None);
        routineId = plan.RegisterRoutine(registration, Catalog(), LocalDateOfLateUtcInstant()).Value;
        return plan;
    }

    public static RoutineParameterCatalog Catalog()
    {
        RoutineTypeParameters parameters = new(
            RoutineTypeCode.Create(OilChangeCode).Value,
            OilChangeName,
            MaintenanceInterval.Create(OilChangeKm, OilChangeMonths).Value,
            DueSoonThreshold.Create(DueSoonPercent).Value);
        return new RoutineParameterCatalog([KeyValuePair.Create(OilChangeId, parameters)]);
    }
}
