using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record GetServiceOrderDetailQuery(ServiceOrderId ServiceOrderId, ServiceOrderActor? Actor) : IQuery<ServiceOrderDetailDto>;
