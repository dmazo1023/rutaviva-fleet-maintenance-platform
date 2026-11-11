using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Bulk;

public sealed class CommittingBulkExecutionScope : IBulkExecutionScope
{
    public IUnitOfWork Resolve(IUnitOfWork realUnitOfWork)
    {
        ArgumentNullException.ThrowIfNull(realUnitOfWork);
        return realUnitOfWork;
    }
}
