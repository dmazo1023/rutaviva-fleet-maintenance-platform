using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Domain.Common;

public static class VehicleErrors
{
    private const string IdentifierEmptyCode = "Identifier.Empty";
    private const string InvalidPlateCode = "Vehicle.InvalidPlate";
    private const string InvalidVinCode = "Vehicle.InvalidVin";
    private const string BrandRequiredCode = "Vehicle.BrandRequired";
    private const string ModelRequiredCode = "Vehicle.ModelRequired";
    private const string InvalidModelYearCode = "Vehicle.InvalidModelYear";
    private const string InvalidOdometerCode = "Vehicle.InvalidOdometer";
    private const string OwnerNotFoundCode = "Vehicle.OwnerNotFound";
    private const string OwnerInactiveCode = "Vehicle.OwnerInactive";
    private const string WorkshopNotFoundCode = "Vehicle.WorkshopNotFound";
    private const string WorkshopInactiveCode = "Vehicle.WorkshopInactive";
    private const string ResponsibleNotFoundCode = "Vehicle.ResponsibleNotFound";
    private const string ResponsibleInactiveCode = "Vehicle.ResponsibleInactive";
    private const string ResponsibleAmbiguousCode = "Vehicle.ResponsibleAmbiguous";
    private const string PlateAlreadyRegisteredCode = "Vehicle.PlateAlreadyRegistered";
    private const string VinAlreadyRegisteredCode = "Vehicle.VinAlreadyRegistered";

    private const string IdentifierEmptyMessage = "The identifier cannot be an empty value.";
    private const string InvalidPlateMessage = "The plate must have three letters, two digits and one digit or letter.";
    private const string InvalidVinMessage = "The VIN must have 17 alphanumeric characters without I, O or Q.";
    private const string BrandRequiredMessage = "The vehicle brand is required.";
    private const string ModelRequiredMessage = "The vehicle model is required.";
    private const string InvalidModelYearMessage = "The model year must be between 1950 and next year.";
    private const string InvalidOdometerMessage = "The odometer kilometers cannot be negative.";
    private const string OwnerNotFoundMessage = "The owner customer was not found.";
    private const string OwnerInactiveMessage = "The owner customer is inactive.";
    private const string WorkshopNotFoundMessage = "The workshop was not found.";
    private const string WorkshopInactiveMessage = "The workshop is inactive.";
    private const string ResponsibleNotFoundMessage = "The responsible approver was not found.";
    private const string ResponsibleInactiveMessage = "The responsible approver is inactive.";
    private const string ResponsibleAmbiguousMessage = "The responsible email matches approvers of several customers.";
    private const string PlateAlreadyRegisteredMessage = "A vehicle with this plate is already registered.";
    private const string VinAlreadyRegisteredMessage = "A vehicle with this VIN is already registered.";

    public static readonly Error IdentifierEmpty = new(IdentifierEmptyCode, IdentifierEmptyMessage, ErrorType.Validation);

    public static readonly Error InvalidPlate = new(InvalidPlateCode, InvalidPlateMessage, ErrorType.Validation);

    public static readonly Error InvalidVin = new(InvalidVinCode, InvalidVinMessage, ErrorType.Validation);

    public static readonly Error BrandRequired = new(BrandRequiredCode, BrandRequiredMessage, ErrorType.Validation);

    public static readonly Error ModelRequired = new(ModelRequiredCode, ModelRequiredMessage, ErrorType.Validation);

    public static readonly Error InvalidModelYear = new(InvalidModelYearCode, InvalidModelYearMessage, ErrorType.Validation);

    public static readonly Error InvalidOdometer = new(InvalidOdometerCode, InvalidOdometerMessage, ErrorType.Validation);

    public static readonly Error OwnerNotFound = new(OwnerNotFoundCode, OwnerNotFoundMessage, ErrorType.BusinessRule);

    public static readonly Error OwnerInactive = new(OwnerInactiveCode, OwnerInactiveMessage, ErrorType.BusinessRule);

    public static readonly Error WorkshopNotFound = new(WorkshopNotFoundCode, WorkshopNotFoundMessage, ErrorType.BusinessRule);

    public static readonly Error WorkshopInactive = new(WorkshopInactiveCode, WorkshopInactiveMessage, ErrorType.BusinessRule);

    public static readonly Error ResponsibleNotFound = new(ResponsibleNotFoundCode, ResponsibleNotFoundMessage, ErrorType.BusinessRule);

    public static readonly Error ResponsibleInactive = new(ResponsibleInactiveCode, ResponsibleInactiveMessage, ErrorType.BusinessRule);

    public static readonly Error ResponsibleAmbiguous = new(ResponsibleAmbiguousCode, ResponsibleAmbiguousMessage, ErrorType.BusinessRule);

    public static readonly Error PlateAlreadyRegistered = new(PlateAlreadyRegisteredCode, PlateAlreadyRegisteredMessage, ErrorType.Conflict);

    public static readonly Error VinAlreadyRegistered = new(VinAlreadyRegisteredCode, VinAlreadyRegisteredMessage, ErrorType.Conflict);
}
