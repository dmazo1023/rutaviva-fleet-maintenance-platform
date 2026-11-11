using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.Vehicles.ValueObjects;

public sealed partial record Vin : ValueObject
{
    private const string VinPattern = "^[A-HJ-NPR-Z0-9]{17}$";

    private Vin(string value) => Value = value;

    public string Value { get; }

    public static Result<Vin> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return VehicleErrors.InvalidVin;
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (!VinRegex().IsMatch(normalized))
        {
            return VehicleErrors.InvalidVin;
        }

        return new Vin(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(VinPattern, RegexOptions.CultureInvariant)]
    private static partial Regex VinRegex();
}
