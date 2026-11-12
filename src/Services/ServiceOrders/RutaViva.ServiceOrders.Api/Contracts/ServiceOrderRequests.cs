namespace RutaViva.ServiceOrders.Api.Contracts;

public sealed record CreateServiceOrderRequest(Guid VehicleId, Guid RoutineId, string? CreatedBy);

public sealed record QuoteServiceOrderRequest(decimal? Amount, string? QuotedBy, Guid? WorkshopId);

public sealed record ApproveServiceOrderRequest(string? ApprovedBy, Guid? ApproverId);

public sealed record RejectServiceOrderRequest(string? RejectedBy, Guid? ApproverId);

public sealed record StartServiceOrderRequest(string? StartedBy, Guid? WorkshopId);

public sealed record CancelServiceOrderRequest(string? CancelledBy);

public sealed record CompleteServiceOrderRequest(string? CompletedBy, long? ServicedKm, Guid? WorkshopId);
