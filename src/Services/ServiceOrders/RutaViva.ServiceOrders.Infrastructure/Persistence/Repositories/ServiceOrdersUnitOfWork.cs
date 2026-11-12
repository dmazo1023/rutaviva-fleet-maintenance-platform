using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrdersUnitOfWork(ServiceOrdersDbContext context) : IUnitOfWork
{
    private readonly EfUnitOfWork<ServiceOrdersDbContext> _inner = new(context);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        Result result = await _inner.SaveChangesAsync(cancellationToken);
        if (result.IsFailure)
        {
            context.ChangeTracker.Clear();
        }

        return result;
    }
}
