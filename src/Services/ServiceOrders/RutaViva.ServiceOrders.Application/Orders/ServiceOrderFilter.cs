using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record ServiceOrderFilter(
    Guid? CustomerId,
    Guid? VehicleId,
    Guid? RoutineId,
    Guid? WorkshopId,
    IReadOnlyCollection<ServiceOrderStatus> Statuses);
