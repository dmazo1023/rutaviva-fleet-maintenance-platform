using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.IntegrationEvents.Outbound;

public sealed class VehicleResponsibleAssignedMapper : IIntegrationEventMapper<VehicleResponsibleAssigned>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(VehicleResponsibleAssigned domainEvent) =>
        [
            new VehicleResponsibleAssignedV1(
                domainEvent.VehicleId.Value,
                domainEvent.ResponsibleApproverId?.Value,
                domainEvent.OccurredAt)
        ];
}
