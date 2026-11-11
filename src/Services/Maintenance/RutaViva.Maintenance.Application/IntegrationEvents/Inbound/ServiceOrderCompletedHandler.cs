using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Inbound;

public sealed class ServiceOrderCompletedHandler(
    ICommandHandler<RestartRoutineCommand, RoutineId> restartRoutine,
    ILogger<ServiceOrderCompletedHandler> logger) : IIntegrationEventHandler<ServiceOrderCompletedV1>
{
    private const string UnknownRoutineTemplate =
        "ServiceOrderCompletedForUnknownRoutine: order {ServiceOrderId}, vehicle {VehicleId}, routine {RoutineId}, error {ErrorCode}";

    public async Task<Result> HandleAsync(ServiceOrderCompletedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        foreach (Guid routineId in RoutineIdsOf(integrationEvent.Order))
        {
            Result restarted = await RestartAsync(integrationEvent, routineId, cancellationToken);
            if (restarted.IsFailure)
            {
                return restarted;
            }
        }

        return Result.Success();
    }

    private static Guid[] RoutineIdsOf(ServiceOrderSnapshot order) => order.RoutineId is { } routineId ? [routineId] : [];

    private static bool IsUnknownTarget(Error error) =>
        error == MaintenanceErrors.MaintenancePlanNotFound || error == MaintenanceErrors.RoutineNotFound;

    private static Result<RestartRoutineCommand> ToCommand(ServiceOrderCompletedV1 integrationEvent, Guid routineId)
    {
        Result<VehicleId> vehicleId = VehicleId.Create(integrationEvent.Order.VehicleId);
        if (vehicleId.IsFailure)
        {
            return vehicleId.Error;
        }

        Result<RoutineId> id = RoutineId.Create(routineId);
        if (id.IsFailure)
        {
            return id.Error;
        }

        return new RestartRoutineCommand(
            vehicleId.Value,
            id.Value,
            integrationEvent.ServicedKm,
            integrationEvent.ServicedAt,
            integrationEvent.Order.ServiceOrderId);
    }

    private async Task<Result> RestartAsync(ServiceOrderCompletedV1 integrationEvent, Guid routineId, CancellationToken cancellationToken)
    {
        Result<RestartRoutineCommand> command = ToCommand(integrationEvent, routineId);
        if (command.IsFailure)
        {
            return Result.Failure(command.Error);
        }

        Result<RoutineId> restarted = await restartRoutine.HandleAsync(command.Value, cancellationToken);
        if (restarted.IsSuccess || !IsUnknownTarget(restarted.Error))
        {
            return restarted;
        }

        logger.LogWarning(
            UnknownRoutineTemplate,
            integrationEvent.Order.ServiceOrderId,
            integrationEvent.Order.VehicleId,
            routineId,
            restarted.Error.Code);
        return Result.Success();
    }
}
