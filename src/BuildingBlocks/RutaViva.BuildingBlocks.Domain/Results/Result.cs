namespace RutaViva.BuildingBlocks.Domain.Results;

public class Result
{
    private const string ErrorOnSuccessMessage = "A successful result has no error.";

    private readonly Error? _error;

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException(ErrorOnSuccessMessage);

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));
}
