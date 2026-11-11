using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Inbound;

public sealed class VehicleRegisteredHandler(ICommandHandler<CreateMaintenancePlanCommand, VehicleId> createPlan)
    : IIntegrationEventHandler<VehicleRegisteredV1>
{
    public async Task<Result> HandleAsync(VehicleRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<CreateMaintenancePlanCommand> command = ToCommand(integrationEvent);
        if (command.IsFailure)
        {
            return Result.Failure(command.Error);
        }

        Result<VehicleId> created = await createPlan.HandleAsync(command.Value, cancellationToken);
        return created;
    }

    private static Result<CreateMaintenancePlanCommand> ToCommand(VehicleRegisteredV1 integrationEvent)
    {
        Result<VehicleId> vehicleId = VehicleId.Create(integrationEvent.VehicleId);
        if (vehicleId.IsFailure)
        {
            return vehicleId.Error;
        }

        Result<CustomerId> customerId = CustomerId.Create(integrationEvent.CustomerId);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        return new CreateMaintenancePlanCommand(
            vehicleId.Value,
            integrationEvent.Plate,
            customerId.Value,
            VehiclePlanStatusMapper.ToPlanStatus(integrationEvent.Status),
            integrationEvent.InitialOdometerKm,
            integrationEvent.RegisteredAt);
    }
}
