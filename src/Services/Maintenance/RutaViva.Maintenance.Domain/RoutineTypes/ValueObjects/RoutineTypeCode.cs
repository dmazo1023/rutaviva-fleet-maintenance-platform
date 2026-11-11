using System.Text.RegularExpressions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

public sealed partial record RoutineTypeCode : ValueObject
{
    private const string CodePattern = "^[A-Z][A-Z0-9_]{0,29}$";

    private RoutineTypeCode(string value) => Value = value;

    public string Value { get; }

    public static Result<RoutineTypeCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MaintenanceErrors.RoutineTypeCodeInvalid;
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (!CodeRegex().IsMatch(normalized))
        {
            return MaintenanceErrors.RoutineTypeCodeInvalid;
        }

        return new RoutineTypeCode(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(CodePattern, RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
