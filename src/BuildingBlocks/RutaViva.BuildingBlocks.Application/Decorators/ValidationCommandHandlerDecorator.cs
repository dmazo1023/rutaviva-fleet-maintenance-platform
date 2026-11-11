using FluentValidation;
using FluentValidation.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private const string ValidationErrorCode = "Validation.Failed";
    private const string MessageSeparator = "; ";

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = await CollectFailuresAsync(command, cancellationToken);
        if (failures.Count > 0)
        {
            return Result<TResult>.Failure(ToError(failures));
        }

        return await inner.HandleAsync(command, cancellationToken);
    }

    private async Task<List<ValidationFailure>> CollectFailuresAsync(TCommand command, CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = [];
        foreach (IValidator<TCommand> validator in validators)
        {
            ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);
            failures.AddRange(validationResult.Errors);
        }

        return failures;
    }

    private static Error ToError(List<ValidationFailure> failures)
    {
        string message = string.Join(MessageSeparator, failures.Select(failure => failure.ErrorMessage));
        return new Error(ValidationErrorCode, message, ErrorType.Validation);
    }
}
