using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Bulk;

public interface IBulkExecutionScope
{
    IUnitOfWork Resolve(IUnitOfWork realUnitOfWork);
}
