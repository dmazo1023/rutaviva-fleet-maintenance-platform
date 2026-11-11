using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Inbound;

public sealed class VehicleStatusChangedHandler(ICommandHandler<ChangePlanStatusCommand, VehicleId> changePlanStatus)
    : IIntegrationEventHandler<VehicleStatusChangedV1>
{
    public async Task<Result> HandleAsync(VehicleStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<VehicleId> vehicleId = VehicleId.Create(integrationEvent.VehicleId);
        if (vehicleId.IsFailure)
        {
            return Result.Failure(vehicleId.Error);
        }

        ChangePlanStatusCommand command = new(vehicleId.Value, VehiclePlanStatusMapper.ToPlanStatus(integrationEvent.Status));
        Result<VehicleId> changed = await changePlanStatus.HandleAsync(command, cancellationToken);
        return changed;
    }
}
