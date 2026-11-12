using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record ListServiceOrdersQuery(
    Guid? CustomerId,
    Guid? VehicleId,
    Guid? RoutineId,
    Guid? WorkshopId,
    ServiceOrderStatusGroup? StatusGroup) : IQuery<IReadOnlyList<ServiceOrderListItemDto>>;
