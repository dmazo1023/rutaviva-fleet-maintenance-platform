using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceUnitOfWork(MaintenanceDbContext context) : IUnitOfWork
{
    private readonly EfUnitOfWork<MaintenanceDbContext> _inner = new(context);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        Result result = await _inner.SaveChangesAsync(cancellationToken);
        if (IsConcurrencyConflict(result))
        {
            context.ChangeTracker.Clear();
        }

        return result;
    }

    private static bool IsConcurrencyConflict(Result result) =>
        result.IsFailure && result.Error.Code == ConcurrencyErrors.Conflict.Code;
}
