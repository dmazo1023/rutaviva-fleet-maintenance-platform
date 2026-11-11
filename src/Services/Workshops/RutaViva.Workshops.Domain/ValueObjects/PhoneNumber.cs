using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.ValueObjects;

public sealed record PhoneNumber : ValueObject
{
    public const int MinDigits = 7;
    public const int MaxDigits = 15;

    private const char InternationalPrefix = '+';
    private const int MaxPrefixLength = 1;

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return WorkshopErrors.PhoneInvalid;
        }

        string normalized = value.Trim();
        if (!HasValidShape(normalized))
        {
            return WorkshopErrors.PhoneInvalid;
        }

        return new PhoneNumber(normalized);
    }

    public override string ToString() => Value;

    private static bool HasValidShape(string value)
    {
        string digits = value.TrimStart(InternationalPrefix);
        bool validPrefix = value.Length - digits.Length <= MaxPrefixLength;
        return validPrefix && digits.Length is >= MinDigits and <= MaxDigits && digits.All(char.IsAsciiDigit);
    }
}
