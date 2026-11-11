using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

internal static class AdministrationPlanFactory
{
    private const int ServiceYear = 2026;
    private const int ServiceMonth = 10;
    private const int ServiceDay = 1;

    private static readonly DateOnly LastServiceDate = new(ServiceYear, ServiceMonth, ServiceDay);

    internal static MaintenancePlan OilPlan(long odometerKm, long lastServiceKm, MaintenancePlanStatus status = MaintenancePlanStatus.Active)
    {
        MaintenancePlan plan = new MaintenancePlanBuilder()
            .WithOdometer(odometerKm)
            .WithStatus(status)
            .WithRegisteredOn(SampleFleet.LoadDate)
            .Build();
        RoutineRegistration registration = RoutineRegistration.Create(
            SampleCatalog.IdOf(SampleCatalog.OilChangeCode),
            lastServiceKm,
            LastServiceDate,
            RoutineOverrides.None);
        plan.RegisterRoutine(registration, SampleCatalog.Catalog, SampleFleet.LoadDate);
        SampleFleet.ClearEvents([plan]);
        return plan;
    }
}
