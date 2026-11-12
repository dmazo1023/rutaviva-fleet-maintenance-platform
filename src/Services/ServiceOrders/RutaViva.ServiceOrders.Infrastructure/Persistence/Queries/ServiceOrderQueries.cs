using Microsoft.EntityFrameworkCore;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Application.Orders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Queries;

public sealed class ServiceOrderQueries(ServiceOrdersDbContext context) : IServiceOrderQueries
{
    public async Task<IReadOnlyList<ServiceOrder>> ListAsync(ServiceOrderFilter filter, CancellationToken cancellationToken)
    {
        IQueryable<ServiceOrder> orders = context.ServiceOrders.AsNoTracking();
        orders = filter.CustomerId is { } customerId ? orders.Where(order => order.Reference.CustomerId == customerId) : orders;
        orders = filter.VehicleId is { } vehicleId ? orders.Where(order => order.Reference.VehicleId == vehicleId) : orders;
        orders = filter.RoutineId is { } routineId ? orders.Where(order => order.Reference.RoutineId == routineId) : orders;
        orders = filter.WorkshopId is { } workshopId ? orders.Where(order => order.Reference.WorkshopId == workshopId) : orders;
        orders = filter.Statuses.Count > 0 ? orders.Where(order => filter.Statuses.Contains(order.Status)) : orders;
        return await orders.OrderByDescending(order => order.UpdatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ServiceOrder?> FindAsync(ServiceOrderId id, CancellationToken cancellationToken) =>
        await context.ServiceOrders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
}
