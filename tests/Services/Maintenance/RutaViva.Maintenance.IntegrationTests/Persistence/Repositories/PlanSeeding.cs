using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;

internal static class PlanSeeding
{
    private const string TodayText = "2026-11-12";
    private const string OilServiceDateText = "2026-08-01";
    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const long OdometerKilometers = 52300L;
    private const long OilServiceKilometers = 45000L;

    internal static async Task<MaintenancePlan> SavePlanAsync(
        SqlServerTestDatabase database,
        string plate,
        CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        List<RoutineType> types = await context.RoutineTypes.ToListAsync(cancellationToken);
        MaintenancePlan plan = CreatePlan(plate);
        RegisterOilChange(plan, RoutineParameterCatalog.From(types));
        context.MaintenancePlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    private static MaintenancePlan CreatePlan(string plate)
    {
        DateTimeOffset readAt = DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture);
        return MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(plate).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(OdometerKilometers, readAt).Value,
            DateOnly.Parse(TodayText, CultureInfo.InvariantCulture));
    }

    private static void RegisterOilChange(MaintenancePlan plan, RoutineParameterCatalog catalog)
    {
        DateOnly today = DateOnly.Parse(TodayText, CultureInfo.InvariantCulture);
        DateOnly oilServiceDate = DateOnly.Parse(OilServiceDateText, CultureInfo.InvariantCulture);
        RoutineRegistration oilChange = RoutineRegistration.Create(
            RoutineTypeSeed.OilChangeId,
            OilServiceKilometers,
            oilServiceDate,
            RoutineOverrides.None);
        plan.RegisterRoutine(oilChange, catalog, today).IsSuccess.Should().BeTrue();
    }
}
