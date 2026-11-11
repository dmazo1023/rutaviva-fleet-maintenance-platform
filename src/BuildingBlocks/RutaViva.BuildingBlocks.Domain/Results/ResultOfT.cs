namespace RutaViva.BuildingBlocks.Domain.Results;

public sealed class Result<T> : Result
{
    private const string ValueOnFailureMessage = "A failed result has no value.";

    private readonly T? _value;

    private Result(T? value, Error? error)
        : base(error is null, error)
    {
        _value = value;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException(ValueOnFailureMessage);

    public static Result<T> Success(T value) => new(value, null);

    public new static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
