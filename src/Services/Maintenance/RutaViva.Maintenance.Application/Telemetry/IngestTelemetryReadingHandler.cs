using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed class IngestTelemetryReadingHandler(
    IMaintenancePlanRepository plans,
    ICommandHandler<RecordOdometerReadingCommand, TelemetryReadingOutcome> recorder,
    ILogger<IngestTelemetryReadingHandler> logger)
    : ICommandHandler<IngestTelemetryReadingCommand, TelemetryReadingOutcome>
{
    private const string UnknownVehicleTemplate = "OdometerReadingForUnknownVehicle received for plate {Plate} with {ReceivedKm} km";

    private static readonly TelemetryReadingOutcome UnknownVehicle =
        new(OdometerReadingOutcome.Ignored, OdometerReadingIgnoredReason.UnknownVehicle);

    public async Task<Result<TelemetryReadingOutcome>> HandleAsync(
        IngestTelemetryReadingCommand command,
        CancellationToken cancellationToken)
    {
        RecordOdometerReadingCommand? record = await TelemetryReadingTranslator.TranslateAsync(command, plans, cancellationToken);
        if (record is null)
        {
            logger.LogWarning(UnknownVehicleTemplate, command.Plate, command.OdometerKm);
            return UnknownVehicle;
        }

        return await recorder.HandleAsync(record, cancellationToken);
    }
}
