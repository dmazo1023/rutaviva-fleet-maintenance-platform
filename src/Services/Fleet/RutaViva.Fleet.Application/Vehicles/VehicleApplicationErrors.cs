using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.Vehicles;

public static class VehicleApplicationErrors
{
    private const string NotFoundCode = "Vehicle.NotFound";
    private const string InvalidStatusCode = "Vehicle.InvalidStatus";
    private const string NotFoundMessage = "The vehicle was not found.";
    private const string InvalidStatusMessage = "The target vehicle status is not supported.";

    public static readonly Error NotFound = new(NotFoundCode, NotFoundMessage, ErrorType.NotFound);

    public static readonly Error InvalidStatus = new(InvalidStatusCode, InvalidStatusMessage, ErrorType.Validation);
}
