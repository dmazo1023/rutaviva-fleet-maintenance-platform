using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Abstractions;

public interface IServiceOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(ServiceOrderId id, CancellationToken cancellationToken);

    Task<bool> HasOpenOrderForRoutineAsync(Guid routineId, CancellationToken cancellationToken);

    void Add(ServiceOrder order);
}
