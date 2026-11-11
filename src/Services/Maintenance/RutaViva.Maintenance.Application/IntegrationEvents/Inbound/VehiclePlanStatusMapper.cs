using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Inbound;

internal static class VehiclePlanStatusMapper
{
    private const string InactiveVehicleStatus = "Inactive";

    public static MaintenancePlanStatus ToPlanStatus(string vehicleStatus)
    {
        if (string.Equals(vehicleStatus, InactiveVehicleStatus, StringComparison.OrdinalIgnoreCase))
        {
            return MaintenancePlanStatus.Suspended;
        }

        return MaintenancePlanStatus.Active;
    }
}
