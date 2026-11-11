using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.IntegrationEvents.Outbound;

public sealed class VehicleStatusChangedMapper : IIntegrationEventMapper<VehicleStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(VehicleStatusChanged domainEvent) =>
        [
            new VehicleStatusChangedV1(
                domainEvent.VehicleId.Value,
                domainEvent.Plate.Value,
                domainEvent.CustomerId.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
