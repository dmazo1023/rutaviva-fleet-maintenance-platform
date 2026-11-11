using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.ValueObjects;

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
            return WorkshopErrors.EmailInvalid;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength || !EmailRegex().IsMatch(normalized))
        {
            return WorkshopErrors.EmailInvalid;
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(EmailPattern, RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
