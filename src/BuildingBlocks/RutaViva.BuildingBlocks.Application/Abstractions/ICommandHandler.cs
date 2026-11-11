using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
