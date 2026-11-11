using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Application.References;

internal static class ReferenceStatusParser
{
    public static Result<ReferenceStatus> Parse(string? value)
    {
        if (string.IsNullOrEmpty(value) || !Enum.IsDefined(typeof(ReferenceStatus), value))
        {
            return ReferenceErrors.InvalidStatus;
        }

        return Enum.Parse<ReferenceStatus>(value);
    }
}
