using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.ValueObjects;

public sealed partial record EmailAddress : ValueObject
{
    public const int MaxLength = 254;

    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CustomerErrors.EmailAddressInvalid;
        }

        string normalized = Normalize(value);
        if (normalized.Length > MaxLength)
        {
            return CustomerErrors.EmailAddressTooLong;
        }

        if (!EmailRegex().IsMatch(normalized))
        {
            return CustomerErrors.EmailAddressInvalid;
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    [GeneratedRegex(EmailPattern, RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
