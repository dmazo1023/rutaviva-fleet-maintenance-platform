using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed record CreateServiceOrderCommand(ServiceOrderReference Reference, string? CreatedBy) : ICommand<ServiceOrderId>;
