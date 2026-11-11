namespace RutaViva.IntegrationContracts.Bulk;

public static class BulkErrorCodes
{
    public const string DuplicateInBatch = "Bulk.DuplicateInBatch";

    public const string CustomerNotFound = "Customer.NotFound";

    public const string VehicleOwnerNotFound = "Vehicle.OwnerNotFound";

    public const string VehicleWorkshopNotFound = "Vehicle.WorkshopNotFound";

    public const string VehicleResponsibleNotFound = "Vehicle.ResponsibleNotFound";

    public const string VehicleResponsibleAmbiguous = "Vehicle.ResponsibleAmbiguous";

    public const string MaintenancePlanNotFound = "MaintenancePlan.NotFound";

    public const string RoutineTypeNotFound = "RoutineType.NotFound";

    public const string RoutineLastServiceKmAboveOdometer = "Routine.LastServiceKmAboveOdometer";

    public const string RoutineIncompleteLastService = "Routine.IncompleteLastService";
}
