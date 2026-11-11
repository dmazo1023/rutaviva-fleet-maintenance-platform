using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.ValueObjects;

public sealed partial record PhoneNumber : ValueObject
{
    private const string PhonePattern = @"^\+?[0-9]{7,15}$";

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CustomerErrors.PhoneNumberInvalid;
        }

        string normalized = Normalize(value);
        if (!PhoneRegex().IsMatch(normalized))
        {
            return CustomerErrors.PhoneNumberInvalid;
        }

        return new PhoneNumber(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    [GeneratedRegex(PhonePattern, RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
