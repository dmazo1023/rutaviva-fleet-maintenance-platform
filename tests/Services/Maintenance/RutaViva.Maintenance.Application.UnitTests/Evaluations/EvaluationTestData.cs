using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.Evaluations;

internal static class EvaluationTestData
{
    internal const string DueSoonServiceDateText = "2026-11-01";
    internal const string OverdueServiceDateText = "2026-10-01";
    internal const string AsOfDateText = "2027-04-20";

    private const string PlateValue = "RVA101";
    private const string OilChangeCode = "OIL_CHANGE";
    private const string OilChangeName = "Oil change";
    private const string RegisteredOnText = "2026-11-12";
    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const int OilChangeKm = 10000;
    private const int OilChangeMonths = 6;
    private const decimal DueSoonPercent = 10m;
    private const long InitialKm = 0;

    private static readonly RoutineTypeId OilChangeId = RoutineTypeId.New();
    private static readonly DateOnly RegisteredOn = DateOnly.Parse(RegisteredOnText, CultureInfo.InvariantCulture);
    private static readonly DateTimeOffset ReadAt = DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture);

    internal static DateOnly AsOfDate => DateOnly.Parse(AsOfDateText, CultureInfo.InvariantCulture);

    internal static RoutineParameterCatalog Catalog() =>
        new(
        [
            KeyValuePair.Create(
                OilChangeId,
                new RoutineTypeParameters(
                    RoutineTypeCode.Create(OilChangeCode).Value,
                    OilChangeName,
                    MaintenanceInterval.Create(OilChangeKm, OilChangeMonths).Value,
                    DueSoonThreshold.Create(DueSoonPercent).Value))
        ]);

    internal static MaintenancePlan PlanWithoutRoutines() => CreatePlan();

    internal static MaintenancePlan PlanWithRoutineServicedOn(string serviceDateText)
    {
        MaintenancePlan plan = CreatePlan();
        DateOnly serviceDate = DateOnly.Parse(serviceDateText, CultureInfo.InvariantCulture);
        RoutineRegistration registration = RoutineRegistration.Create(
            OilChangeId,
            ServiceRecord.Create(InitialKm, serviceDate).Value,
            RoutineOverrides.None);
        Result<RoutineId> registered = plan.RegisterRoutine(registration, Catalog(), RegisteredOn);
        registered.IsSuccess.Should().BeTrue();
        return plan;
    }

    private static MaintenancePlan CreatePlan() =>
        MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(PlateValue).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(InitialKm, ReadAt).Value,
            RegisteredOn);
}
