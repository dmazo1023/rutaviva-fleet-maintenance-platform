using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.References;

public static class ReferenceErrors
{
    private const string InvalidStatusCode = "Reference.InvalidStatus";
    private const string InvalidStatusMessage = "The replicated status is not a known reference status.";

    public static readonly Error InvalidStatus = new(InvalidStatusCode, InvalidStatusMessage, ErrorType.Validation);
}
