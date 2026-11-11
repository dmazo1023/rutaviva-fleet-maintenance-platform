using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

internal static class QueryPlanSeeder
{
    private const string TodayText = "2026-11-12";
    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const string Author = "integration-tests";

    internal static async Task<MaintenancePlan> SaveAsync(
        SqlServerTestDatabase database,
        string plate,
        CustomerId customerId,
        long odometerKilometers,
        IReadOnlyList<RoutineSeed> routines,
        CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        List<RoutineType> types = await context.RoutineTypes.ToListAsync(cancellationToken);
        RoutineParameterCatalog catalog = RoutineParameterCatalog.From(types);
        MaintenancePlan plan = CreatePlan(plate, customerId, odometerKilometers);
        foreach (RoutineSeed routine in routines)
        {
            Register(plan, routine, catalog);
        }

        context.MaintenancePlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    private static DateOnly Today() => DateOnly.Parse(TodayText, CultureInfo.InvariantCulture);

    private static MaintenancePlan CreatePlan(string plate, CustomerId customerId, long odometerKilometers)
    {
        DateTimeOffset readAt = DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture);
        return MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(plate).Value,
            customerId,
            MaintenancePlanStatus.Active,
            Odometer.Create(odometerKilometers, readAt).Value,
            Today());
    }

    private static void Register(MaintenancePlan plan, RoutineSeed seed, RoutineParameterCatalog catalog)
    {
        DateOnly serviceDate = DateOnly.Parse(seed.LastServiceDate, CultureInfo.InvariantCulture);
        RoutineRegistration registration = RoutineRegistration.Create(
            seed.TypeId,
            seed.LastServiceKilometers,
            serviceDate,
            RoutineOverrides.None);
        Result<RoutineId> registered = plan.RegisterRoutine(registration, catalog, Today());
        registered.IsSuccess.Should().BeTrue();
        Deactivate(plan, registered.Value, seed, catalog);
    }

    private static void Deactivate(MaintenancePlan plan, RoutineId routineId, RoutineSeed seed, RoutineParameterCatalog catalog)
    {
        if (seed.IsActive)
        {
            return;
        }

        plan.ChangeRoutineState(routineId, RoutineState.Inactive, Author, catalog, Today()).IsSuccess.Should().BeTrue();
    }
}
