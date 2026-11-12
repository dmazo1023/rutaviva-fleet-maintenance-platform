using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Events;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Outbound;

public sealed class ServiceOrderCompletedMapper : IIntegrationEventMapper<ServiceOrderCompleted>
{
    private const string Currency = "COP";

    public IReadOnlyCollection<IIntegrationEvent> Map(ServiceOrderCompleted domainEvent) =>
        [new ServiceOrderCompletedV1(SnapshotOf(domainEvent), domainEvent.ServicedKm, domainEvent.OccurredAt, domainEvent.OccurredAt)];

    private static ServiceOrderSnapshot SnapshotOf(ServiceOrderCompleted domainEvent)
    {
        ServiceOrderReference reference = domainEvent.Reference;
        return new ServiceOrderSnapshot(
            domainEvent.ServiceOrderId.Value,
            domainEvent.Number,
            domainEvent.Origin.ToString(),
            reference.VehicleId,
            reference.Plate,
            reference.RoutineId,
            reference.RoutineName,
            reference.CustomerId,
            reference.WorkshopId,
            reference.ResponsibleApproverId,
            nameof(ServiceOrderStatus.Completed),
            null,
            null,
            null,
            domainEvent.TotalAmount,
            null,
            domainEvent.TotalAmount,
            Currency,
            domainEvent.AutoApproved,
            null,
            null,
            domainEvent.CreatedAt);
    }
}
