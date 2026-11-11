using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.IntegrationEvents.Outbound;

public sealed class VehicleRegisteredMapper : IIntegrationEventMapper<VehicleRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(VehicleRegistered domainEvent) =>
        [
            new VehicleRegisteredV1(
                domainEvent.VehicleId.Value,
                domainEvent.Plate.Value,
                domainEvent.Vin?.Value,
                domainEvent.Description.Brand,
                domainEvent.Description.Model,
                domainEvent.Description.ModelYear,
                domainEvent.CustomerId.Value,
                domainEvent.WorkshopId.Value,
                domainEvent.ResponsibleApproverId?.Value,
                domainEvent.Status.ToString(),
                domainEvent.InitialOdometer.Value,
                domainEvent.RegisteredAt,
                domainEvent.OccurredAt)
        ];
}
