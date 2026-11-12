using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record ExecuteServiceOrderActionCommand(
    ServiceOrderId ServiceOrderId,
    ServiceOrderAction Action,
    string? Actor,
    decimal? Amount,
    long? ServicedKm,
    Guid? ApproverId,
    Guid? WorkshopId) : ICommand<ServiceOrderId>;
