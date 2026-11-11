using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        Error? error = await RequestValidation.ValidateAsync(validators, command, cancellationToken);
        if (error is not null)
        {
            return Result<TResult>.Failure(error);
        }

        return await inner.HandleAsync(command, cancellationToken);
    }
}
