namespace RutaViva.Maintenance.Api.Contracts;

public sealed record UpdateRoutineTypeParametersRequest(
    int? IntervalKm,
    int? IntervalMonths,
    decimal DueSoonThresholdPercent,
    string? UpdatedBy);
