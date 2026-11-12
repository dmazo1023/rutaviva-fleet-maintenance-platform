using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Infrastructure.Persistence.Repositories;

public sealed class WorkshopsUnitOfWork(WorkshopsDbContext context) : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly EfUnitOfWork<WorkshopsDbContext> _inner = new(context);

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Result result = await _inner.SaveChangesAsync(cancellationToken);
            ClearTrackerWhenFailed(result);
            return result;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            context.ChangeTracker.Clear();
            return Result.Failure(WorkshopErrors.TaxIdAlreadyRegistered);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };

    private void ClearTrackerWhenFailed(Result result)
    {
        if (result.IsFailure)
        {
            context.ChangeTracker.Clear();
        }
    }
}
