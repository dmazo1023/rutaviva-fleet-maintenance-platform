using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.ValueObjects;

public sealed partial record TaxId : ValueObject
{
    private const string TaxIdPattern = "^[0-9]{6,12}(-[0-9])?$";

    private TaxId(string value) => Value = value;

    public string Value { get; }

    public static Result<TaxId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CustomerErrors.TaxIdInvalid;
        }

        string normalized = Normalize(value);
        if (!TaxIdRegex().IsMatch(normalized))
        {
            return CustomerErrors.TaxIdInvalid;
        }

        return new TaxId(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    [GeneratedRegex(TaxIdPattern, RegexOptions.CultureInvariant)]
    private static partial Regex TaxIdRegex();
}
