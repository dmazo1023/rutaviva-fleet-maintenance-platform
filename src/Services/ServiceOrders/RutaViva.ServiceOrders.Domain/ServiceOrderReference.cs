namespace RutaViva.ServiceOrders.Domain;

public sealed record ServiceOrderReference(
    Guid VehicleId,
    string Plate,
    Guid CustomerId,
    Guid WorkshopId,
    string WorkshopName,
    Guid RoutineId,
    string RoutineName,
    string RoutineTypeCode);
