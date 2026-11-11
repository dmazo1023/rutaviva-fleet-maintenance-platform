using System.Runtime.CompilerServices;
using RutaViva.BuildingBlocks.Domain.Guards;

namespace RutaViva.EventBus.Infrastructure.Persistence;

internal static class BoundedText
{
    public static string Require(string? value, int maxLength, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        string text = Guard.NotNullOrWhiteSpace(value, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, maxLength, parameterName);
        return text;
    }
}
