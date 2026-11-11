using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Bulk;

public sealed class DryRunBulkExecutionScope : IBulkExecutionScope
{
    private readonly DryRunUnitOfWork _dryRunUnitOfWork = new();

    public IUnitOfWork Resolve(IUnitOfWork realUnitOfWork) => _dryRunUnitOfWork;
}
