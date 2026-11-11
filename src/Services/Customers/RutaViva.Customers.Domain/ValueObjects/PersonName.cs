using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.ValueObjects;

public sealed record PersonName : ValueObject
{
    public const int MaxLength = 200;

    private PersonName(string value) => Value = value;

    public string Value { get; }

    public static Result<PersonName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CustomerErrors.PersonNameRequired;
        }

        string normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            return CustomerErrors.PersonNameTooLong;
        }

        return new PersonName(normalized);
    }

    public override string ToString() => Value;
}
