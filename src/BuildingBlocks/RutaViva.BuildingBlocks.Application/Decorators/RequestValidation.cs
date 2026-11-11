using FluentValidation;
using FluentValidation.Results;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

internal static class RequestValidation
{
    private const string ValidationErrorCode = "Validation.Failed";
    private const string MessageSeparator = "; ";

    public static async Task<Error?> ValidateAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = await CollectFailuresAsync(validators, request, cancellationToken);
        if (failures.Count == 0)
        {
            return null;
        }

        return ToError(failures);
    }

    private static async Task<List<ValidationFailure>> CollectFailuresAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = [];
        foreach (IValidator<TRequest> validator in validators)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
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
