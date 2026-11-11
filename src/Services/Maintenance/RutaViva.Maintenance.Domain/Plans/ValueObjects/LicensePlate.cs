using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed partial record LicensePlate : ValueObject
{
    private const string PlatePattern = "^[A-Z]{3}[0-9]{2}[0-9A-Z]$";

    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    public static Result<LicensePlate> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MaintenanceErrors.LicensePlateInvalid;
        }

        string normalized = Normalize(value);
        if (!PlateRegex().IsMatch(normalized))
        {
            return MaintenanceErrors.LicensePlateInvalid;
        }

        return new LicensePlate(normalized);
    }

    public override string ToString() => Value;

    private static string Normalize(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();

    [GeneratedRegex(PlatePattern, RegexOptions.CultureInvariant)]
    private static partial Regex PlateRegex();
}
