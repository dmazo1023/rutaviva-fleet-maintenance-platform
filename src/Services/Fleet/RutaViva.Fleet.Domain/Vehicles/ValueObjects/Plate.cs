using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.Vehicles.ValueObjects;

public sealed partial record Plate : ValueObject
{
    private const string PlatePattern = "^[A-Z]{3}[0-9]{2}[0-9A-Z]$";

    private Plate(string value) => Value = value;

    public string Value { get; }

    public static Result<Plate> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return VehicleErrors.InvalidPlate;
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (!PlateRegex().IsMatch(normalized))
        {
            return VehicleErrors.InvalidPlate;
        }

        return new Plate(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(PlatePattern, RegexOptions.CultureInvariant)]
    private static partial Regex PlateRegex();
}
