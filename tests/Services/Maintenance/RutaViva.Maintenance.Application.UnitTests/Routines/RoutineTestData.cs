using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.Routines;

internal static class RoutineTestData
{
    public const string OilChangeCode = "OIL_CHANGE";
    public const string BrakesCode = "BRAKES";
    public const string SeedUser = "seed";
    public const string OilChangeName = "Oil change";
    public const string BrakesName = "Brakes";
    public const long PlanOdometerKm = 30200;

    private const int OilIntervalKm = 5000;
    private const int OilIntervalMonths = 6;
    private const decimal OilThresholdPercent = 10m;
    private const int BrakesIntervalKm = 30000;
    private const decimal BrakesThresholdPercent = 10m;
    private const int NowYear = 2026;
    private const int NowMonth = 11;
    private const int NowDay = 12;
    private const int NowHour = 15;

    public static readonly DateTimeOffset Now = new(NowYear, NowMonth, NowDay, NowHour, 0, 0, TimeSpan.Zero);

    public static readonly RoutineTypeId OilChangeId = RoutineTypeId.New();

    public static readonly RoutineTypeId BrakesId = RoutineTypeId.New();

    public static DateOnly Today() => MaintenanceTimeZone.ToLocalDate(Now);

    public static RoutineParameterCatalog Catalog() =>
        RoutineParameterCatalog.From([
            CreateType(OilChangeId, OilChangeCode, OilChangeName, OilIntervalKm, OilIntervalMonths, OilThresholdPercent),
            CreateType(BrakesId, BrakesCode, BrakesName, BrakesIntervalKm, null, BrakesThresholdPercent)
        ]);

    public static MaintenancePlan Plan(string plate) =>
        MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(plate).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(PlanOdometerKm, Now).Value,
            Today());

    public static RoutineId AddRoutine(MaintenancePlan plan, RoutineTypeId routineTypeId, RoutineParameterCatalog catalog) =>
        plan.RegisterRoutine(RoutineRegistration.Create(routineTypeId, null, RoutineOverrides.None), catalog, Today()).Value;

    private static RoutineType CreateType(
        RoutineTypeId id,
        string code,
        string name,
        int intervalKm,
        int? intervalMonths,
        decimal thresholdPercent) =>
        RoutineType.Create(
            id,
            RoutineTypeCode.Create(code).Value,
            name,
            MaintenanceInterval.Create(intervalKm, intervalMonths).Value,
            DueSoonThreshold.Create(thresholdPercent).Value,
            SeedUser,
            Now).Value;
}
