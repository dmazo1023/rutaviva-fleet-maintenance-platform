using Microsoft.EntityFrameworkCore;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrderRepository(ServiceOrdersDbContext context) : IServiceOrderRepository
{
    public async Task<ServiceOrder?> GetByIdAsync(ServiceOrderId id, CancellationToken cancellationToken) =>
        await context.ServiceOrders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<bool> HasOpenOrderForRoutineAsync(Guid routineId, CancellationToken cancellationToken) =>
        await context.ServiceOrders.AnyAsync(
            order => order.Reference.RoutineId == routineId && ServiceOrderTransitions.OpenStatuses.Contains(order.Status),
            cancellationToken);

    public void Add(ServiceOrder order) => context.ServiceOrders.Add(order);
}
