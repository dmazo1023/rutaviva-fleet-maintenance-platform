using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record ListServiceOrdersQuery(
    Guid? CustomerId,
    Guid? VehicleId,
    Guid? RoutineId,
    ServiceOrderStatusGroup? StatusGroup) : IQuery<IReadOnlyList<ServiceOrderListItemDto>>;
