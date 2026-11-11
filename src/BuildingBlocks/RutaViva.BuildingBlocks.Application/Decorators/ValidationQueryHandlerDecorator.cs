using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

public sealed class ValidationQueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        Error? error = await RequestValidation.ValidateAsync(validators, query, cancellationToken);
        if (error is not null)
        {
            return Result<TResult>.Failure(error);
        }

        return await inner.HandleAsync(query, cancellationToken);
    }
}
