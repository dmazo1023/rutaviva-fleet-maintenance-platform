using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class SavingBulkCommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<BulkCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(BulkCommand command, CancellationToken cancellationToken)
    {
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return Result<Guid>.Failure(saved.Error);
        }

        return Result<Guid>.Success(Guid.NewGuid());
    }
}
