using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public static class VehicleQueryErrors
{
    public const string InvalidPageCode = "Vehicle.InvalidPage";
    public const string InvalidPageSizeCode = "Vehicle.InvalidPageSize";

    private const string InvalidPageMessage = "The page must be greater than or equal to 1.";
    private const string InvalidPageSizeMessage = "The page size must be between 1 and 200.";

    public static readonly Error InvalidPage = new(InvalidPageCode, InvalidPageMessage, ErrorType.Validation);

    public static readonly Error InvalidPageSize = new(InvalidPageSizeCode, InvalidPageSizeMessage, ErrorType.Validation);
}
