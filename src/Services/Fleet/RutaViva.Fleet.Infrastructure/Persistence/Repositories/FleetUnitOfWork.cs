using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.Fleet.Infrastructure.Persistence.Repositories;

public sealed class FleetUnitOfWork(FleetDbContext context) : IUnitOfWork
{
    private readonly EfUnitOfWork<FleetDbContext> _inner = new(context);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Result result = await _inner.SaveChangesAsync(cancellationToken);
            ClearWhenConflict(result);
            return result;
        }
        catch (DbUpdateException exception) when (UniqueViolationTranslator.TryTranslate(exception, out Error? error))
        {
            context.ChangeTracker.Clear();
            return Result.Failure(error);
        }
    }

    private void ClearWhenConflict(Result result)
    {
        if (result.IsFailure && result.Error.Code == ConcurrencyErrors.Conflict.Code)
        {
            context.ChangeTracker.Clear();
        }
    }
}
