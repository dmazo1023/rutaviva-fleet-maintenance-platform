using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.ServiceOrders.Application.Orders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class MaintenanceRoutineOverdueHandler(
    ICommandHandler<CreateServiceOrderCommand, ServiceOrderId> createOrder,
    AutomaticOrderPolicy automaticOrders,
    ILogger<MaintenanceRoutineOverdueHandler> logger) : IIntegrationEventHandler<MaintenanceRoutineOverdueV1>
{
    private const string SystemActor = "Sistema";
    private const string CreatedTemplate = "AutomaticServiceOrderCreated: order {ServiceOrderId}, vehicle {VehicleId}, routine {RoutineId}";
    private const string SkippedTemplate = "AutomaticServiceOrderSkipped: vehicle {VehicleId}, routine {RoutineId}, reason {Reason}";

    private static readonly Error[] SkippableErrors =
    [
        ServiceOrderErrors.OpenOrderExists,
        ServiceOrderErrors.VehicleNotFound,
        ServiceOrderErrors.RoutineNotFound,
        ServiceOrderErrors.WorkshopNotFound
    ];

    public async Task<Result> HandleAsync(MaintenanceRoutineOverdueV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        RoutineSnapshot routine = integrationEvent.Routine;
        if (!automaticOrders.Applies(integrationEvent.OccurredAt))
        {
            return Result.Success();
        }

        CreateServiceOrderCommand command = new(routine.VehicleId, routine.RoutineId, ServiceOrderOrigin.Automatic, SystemActor);
        Result<ServiceOrderId> created = await createOrder.HandleAsync(command, cancellationToken);
        if (created.IsSuccess)
        {
            logger.LogInformation(CreatedTemplate, created.Value.Value, routine.VehicleId, routine.RoutineId);
            return Result.Success();
        }

        if (!SkippableErrors.Contains(created.Error))
        {
            return Result.Failure(created.Error);
        }

        logger.LogWarning(SkippedTemplate, routine.VehicleId, routine.RoutineId, created.Error.Code);
        return Result.Success();
    }
}
