using System.Runtime.CompilerServices;

namespace RutaViva.BuildingBlocks.Domain.Guards;

public static class Guard
{
    private const string BlankMessage = "The value cannot be null, empty or whitespace.";
    private const string DefaultMessage = "The value cannot be the default value.";
    private const string NotPositiveMessage = "The value must be greater than zero.";

    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class
        => value ?? throw new ArgumentNullException(parameterName);

    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(BlankMessage, parameterName) : value;

    public static T NotDefault<T>(T value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : struct
        => EqualityComparer<T>.Default.Equals(value, default) ? throw new ArgumentException(DefaultMessage, parameterName) : value;

    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        => value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName, value, NotPositiveMessage);
}
