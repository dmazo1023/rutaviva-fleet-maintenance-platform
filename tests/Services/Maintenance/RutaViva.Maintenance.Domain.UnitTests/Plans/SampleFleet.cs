using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

internal static class SampleFleet
{
    internal const string Rva101 = "RVA101";
    internal const string Rva102 = "RVA102";
    internal const string Rva103 = "RVA103";
    internal const string Rva104 = "RVA104";
    internal const string Tan201 = "TAN201";
    internal const string Tan202 = "TAN202";
    internal const string Tan203 = "TAN203";
    internal const string Eva301 = "EVA301";
    internal const string Eva302 = "EVA302";
    internal const string Eva30D = "EVA30D";

    private const int MotorcycleIntervalKm = 3000;
    private const int MotorcycleIntervalMonths = 3;
    private const decimal MotorcycleDueSoonPercent = 20m;

    internal static readonly DateOnly LoadDate = new(2026, 11, 12);

    private static readonly IReadOnlyList<VehicleRow> Vehicles =
    [
        new(Rva101, 52300),
        new(Rva102, 81450),
        new(Rva103, 30200),
        new(Rva104, 98000),
        new(Tan201, 120800),
        new(Tan202, 19000),
        new(Tan203, 70100),
        new(Eva301, 0),
        new(Eva302, 45500),
        new(Eva30D, 12500)
    ];

    private static readonly IReadOnlyList<RoutineRow> Routines =
    [
        new(Rva101, SampleCatalog.OilChangeCode, 45000, new DateOnly(2026, 8, 1), RoutineOverrides.None),
        new(Rva101, SampleCatalog.BrakesCode, 40000, new DateOnly(2026, 3, 15), RoutineOverrides.None),
        new(Rva102, SampleCatalog.OilChangeCode, 72000, new DateOnly(2026, 7, 15), RoutineOverrides.None),
        new(Rva103, SampleCatalog.OilChangeCode, 28000, new DateOnly(2026, 5, 25), RoutineOverrides.None),
        new(Rva104, SampleCatalog.OilChangeCode, 92000, new DateOnly(2026, 9, 20), RoutineOverrides.None),
        new(Rva104, SampleCatalog.BrakesCode, 77000, new DateOnly(2025, 12, 1), RoutineOverrides.None),
        new(Tan201, SampleCatalog.OilChangeCode, 110000, new DateOnly(2026, 9, 1), RoutineOverrides.None),
        new(Tan201, SampleCatalog.BrakesCode, 105000, new DateOnly(2026, 6, 10), RoutineOverrides.None),
        new(Tan202, SampleCatalog.OilChangeCode, 15000, new DateOnly(2026, 4, 20), RoutineOverrides.None),
        new(Tan203, SampleCatalog.OilChangeCode, 60000, new DateOnly(2026, 5, 1), RoutineOverrides.None),
        new(Eva301, SampleCatalog.OilChangeCode, null, null, RoutineOverrides.None),
        new(Eva301, SampleCatalog.BrakesCode, null, null, RoutineOverrides.None),
        new(Eva302, SampleCatalog.BrakesCode, 27000, new DateOnly(2026, 2, 10), RoutineOverrides.None),
        new(Eva302, SampleCatalog.TiresCode, 10000, new DateOnly(2025, 8, 1), RoutineOverrides.None),
        new(Eva30D, SampleCatalog.OilChangeCode, 10000, new DateOnly(2026, 10, 1), MotorcycleOverrides())
    ];

    internal static IReadOnlyDictionary<string, MaintenancePlan> Load()
    {
        Dictionary<string, MaintenancePlan> plans = Vehicles.ToDictionary(vehicle => vehicle.Plate, BuildPlan);
        foreach (RoutineRow row in Routines)
        {
            Register(plans[row.Plate], row);
        }

        return plans;
    }

    internal static void ClearEvents(IEnumerable<MaintenancePlan> plans)
    {
        foreach (IAggregateRoot plan in plans)
        {
            plan.ClearDomainEvents();
        }
    }

    internal static MaintenanceRoutine RoutineOf(MaintenancePlan plan, string code) =>
        plan.Routines.Single(routine => routine.RoutineTypeCode.Value == code);

    private static RoutineOverrides MotorcycleOverrides() =>
        RoutineOverrides.Create(MotorcycleIntervalKm, MotorcycleIntervalMonths, MotorcycleDueSoonPercent).Value;

    private static MaintenancePlan BuildPlan(VehicleRow vehicle) =>
        new MaintenancePlanBuilder()
            .WithPlate(vehicle.Plate)
            .WithOdometer(vehicle.OdometerKm)
            .WithRegisteredOn(LoadDate)
            .Build();

    private static void Register(MaintenancePlan plan, RoutineRow row)
    {
        RoutineRegistration registration = RoutineRegistration.Create(
            SampleCatalog.IdOf(row.Code),
            row.LastServiceKm,
            row.LastServiceDate,
            row.Overrides);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, LoadDate);

        result.IsSuccess.Should().BeTrue();
    }

    private sealed record VehicleRow(string Plate, long OdometerKm);

    private sealed record RoutineRow(string Plate, string Code, long? LastServiceKm, DateOnly? LastServiceDate, RoutineOverrides Overrides);
}
