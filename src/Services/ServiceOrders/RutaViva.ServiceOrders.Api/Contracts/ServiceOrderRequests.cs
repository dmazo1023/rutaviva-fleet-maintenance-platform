namespace RutaViva.ServiceOrders.Api.Contracts;

public sealed record CreateServiceOrderRequest(
    Guid VehicleId,
    string? Plate,
    Guid CustomerId,
    Guid WorkshopId,
    string? WorkshopName,
    Guid RoutineId,
    string? RoutineName,
    string? RoutineTypeCode,
    string? CreatedBy);

public sealed record QuoteServiceOrderRequest(decimal? Amount, string? QuotedBy);

public sealed record ApproveServiceOrderRequest(string? ApprovedBy);

public sealed record RejectServiceOrderRequest(string? RejectedBy);

public sealed record StartServiceOrderRequest(string? StartedBy);

public sealed record CompleteServiceOrderRequest(string? CompletedBy, long? ServicedKm);
