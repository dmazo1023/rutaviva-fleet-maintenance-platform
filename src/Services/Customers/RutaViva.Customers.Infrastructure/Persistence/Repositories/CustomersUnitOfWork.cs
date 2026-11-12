using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomersUnitOfWork(CustomersDbContext context) : IUnitOfWork
{
    private readonly EfUnitOfWork<CustomersDbContext> _inner = new(context);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Result result = await _inner.SaveChangesAsync(cancellationToken);
            DiscardTrackedChangesOnConflict(result);
            return result;
        }
        catch (DbUpdateException exception)
        {
            return TranslateOrRethrow(exception);
        }
    }

    private Result TranslateOrRethrow(DbUpdateException exception)
    {
        Error? error = UniqueViolationTranslator.Translate(exception);
        if (error is null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        context.ChangeTracker.Clear();
        return Result.Failure(error!);
    }

    private void DiscardTrackedChangesOnConflict(Result result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        if (result.Error.Code != ConcurrencyErrors.Conflict.Code)
        {
            return;
        }

        context.ChangeTracker.Clear();
    }
}
