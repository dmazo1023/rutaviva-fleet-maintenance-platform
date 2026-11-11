using System.Globalization;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

internal static class TelemetryTestData
{
    internal const string PlateValue = "RVA101";

    private const string ReadAtText = "2026-11-12T08:00:00-05:00";
    private const string RegisteredOnText = "2026-11-12";

    internal static readonly DateTimeOffset ReadAt = DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture);

    private static readonly DateOnly RegisteredOn = DateOnly.Parse(RegisteredOnText, CultureInfo.InvariantCulture);

    internal static MaintenancePlan CreatePlan(long odometerKm) =>
        MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(PlateValue).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(odometerKm, ReadAt).Value,
            RegisteredOn);

    internal static RoutineParameterCatalog EmptyCatalog() => new([]);
}
