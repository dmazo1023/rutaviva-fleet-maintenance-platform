using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed class RecordOdometerReadingHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IOdometerReadingLog readingLog,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RecordOdometerReadingHandler> logger)
    : ICommandHandler<RecordOdometerReadingCommand, TelemetryReadingOutcome>
{
    private const string IgnoredTemplate = "OdometerReadingIgnored for vehicle {VehicleId}: received {ReceivedKm} km, current {CurrentKm} km";

    public Task<Result<TelemetryReadingOutcome>> HandleAsync(
        RecordOdometerReadingCommand command,
        CancellationToken cancellationToken) =>
        SingleRetryOnConcurrencyConflict.ExecuteAsync(token => RecordAsync(command, token), cancellationToken);

    private static OdometerReadingLogEntry ToLogEntry(
        RecordOdometerReadingCommand command,
        OdometerReadingResult result,
        DateTimeOffset receivedAt) =>
        new(command.VehicleId, command.Kilometers, command.ReadAt, receivedAt, result.Outcome, result.IgnoredReason);

    private static OdometerReadingRecordedV1 ToIntegrationEvent(
        MaintenancePlan plan,
        RecordOdometerReadingCommand command,
        OdometerReadingResult result,
        DateTimeOffset occurredAt) =>
        new(
            plan.Id.Value,
            plan.Plate.Value,
            plan.CustomerId.Value,
            command.Kilometers,
            command.ReadAt,
            result.Outcome.ToString(),
            result.IgnoredReason?.ToString(),
            plan.Odometer.Kilometers,
            occurredAt);

    private async Task<Result<TelemetryReadingOutcome>> RecordAsync(
        RecordOdometerReadingCommand command,
        CancellationToken cancellationToken)
    {
        Result<OdometerReading> reading = OdometerReading.Create(command.Kilometers, command.ReadAt);
        if (reading.IsFailure)
        {
            return reading.Error;
        }

        MaintenancePlan? plan = await plans.GetAsync(command.VehicleId, cancellationToken);
        if (plan is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        OdometerReadingResult result = plan.RecordOdometerReading(reading.Value, catalog);
        Persist(plan, command, result);

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return new TelemetryReadingOutcome(result.Outcome, result.IgnoredReason);
    }

    private void Persist(MaintenancePlan plan, RecordOdometerReadingCommand command, OdometerReadingResult result)
    {
        DateTimeOffset receivedAt = clock.UtcNow;
        readingLog.Append(ToLogEntry(command, result, receivedAt));
        publisher.Enqueue(ToIntegrationEvent(plan, command, result, receivedAt), plan.Id.ToString());
        LogIfIgnored(plan, command, result);
    }

    private void LogIfIgnored(MaintenancePlan plan, RecordOdometerReadingCommand command, OdometerReadingResult result)
    {
        if (result.IsAccepted)
        {
            return;
        }

        logger.LogInformation(IgnoredTemplate, plan.Id.Value, command.Kilometers, plan.Odometer.Kilometers);
    }
}
