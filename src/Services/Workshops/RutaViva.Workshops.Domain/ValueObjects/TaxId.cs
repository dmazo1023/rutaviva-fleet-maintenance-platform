using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.ValueObjects;

public sealed record TaxId : ValueObject
{
    public const int MinLength = 9;
    public const int MaxLength = 10;

    private TaxId(string value) => Value = value;

    public string Value { get; }

    public static Result<TaxId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return WorkshopErrors.TaxIdInvalid;
        }

        string normalized = value.Trim();
        if (!HasValidShape(normalized))
        {
            return WorkshopErrors.TaxIdInvalid;
        }

        return new TaxId(normalized);
    }

    public override string ToString() => Value;

    private static bool HasValidShape(string value) =>
        value.Length is >= MinLength and <= MaxLength && value.All(char.IsAsciiDigit);
}
