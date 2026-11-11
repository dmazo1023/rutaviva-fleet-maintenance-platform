using System.Globalization;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

internal sealed class MaintenancePlanBuilder
{
    private const string DefaultPlate = "RVA101";
    private const string DefaultReadAtText = "2026-11-12T00:00:00-05:00";
    private const int DefaultYear = 2026;
    private const int DefaultMonth = 11;
    private const int DefaultDay = 12;

    private static readonly DateTimeOffset DefaultReadAt = DateTimeOffset.Parse(DefaultReadAtText, CultureInfo.InvariantCulture);

    private string _plate = DefaultPlate;
    private long _odometerKm;
    private MaintenancePlanStatus _status = MaintenancePlanStatus.Active;
    private DateOnly _registeredOn = new(DefaultYear, DefaultMonth, DefaultDay);

    internal MaintenancePlanBuilder WithPlate(string plate)
    {
        _plate = plate;
        return this;
    }

    internal MaintenancePlanBuilder WithOdometer(long kilometers)
    {
        _odometerKm = kilometers;
        return this;
    }

    internal MaintenancePlanBuilder WithStatus(MaintenancePlanStatus status)
    {
        _status = status;
        return this;
    }

    internal MaintenancePlanBuilder WithRegisteredOn(DateOnly registeredOn)
    {
        _registeredOn = registeredOn;
        return this;
    }

    internal MaintenancePlan Build() =>
        MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(_plate).Value,
            CustomerId.New(),
            _status,
            Odometer.Create(_odometerKm, DefaultReadAt).Value,
            _registeredOn);
}
