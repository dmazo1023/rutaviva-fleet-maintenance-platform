namespace RutaViva.Maintenance.Api.Contracts;

public sealed record ReevaluationRequest(DateOnly? AsOfDate, Guid? VehicleId);
