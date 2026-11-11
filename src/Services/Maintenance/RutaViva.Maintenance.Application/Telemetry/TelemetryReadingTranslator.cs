using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Application.Telemetry;

public static class TelemetryReadingTranslator
{
    public static async Task<RecordOdometerReadingCommand?> TranslateAsync(
        IngestTelemetryReadingCommand command,
        IMaintenancePlanRepository plans,
        CancellationToken cancellationToken)
    {
        Result<LicensePlate> plate = LicensePlate.Create(command.Plate);
        if (plate.IsFailure)
        {
            return null;
        }

        VehicleId? vehicleId = await plans.FindVehicleIdByPlateAsync(plate.Value, cancellationToken);
        if (vehicleId is null)
        {
            return null;
        }

        return new RecordOdometerReadingCommand(vehicleId.Value, command.OdometerKm, command.ReadAt);
    }
}
