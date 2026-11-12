using RutaViva.ServiceOrders.Application.Orders;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Abstractions;

public interface IServiceOrderQueries
{
    Task<IReadOnlyList<ServiceOrder>> ListAsync(ServiceOrderFilter filter, CancellationToken cancellationToken);

    Task<ServiceOrder?> FindAsync(ServiceOrderId id, CancellationToken cancellationToken);
}
