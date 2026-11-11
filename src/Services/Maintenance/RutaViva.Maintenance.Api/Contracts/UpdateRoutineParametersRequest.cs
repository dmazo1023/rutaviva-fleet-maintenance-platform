namespace RutaViva.Maintenance.Api.Contracts;

public sealed record UpdateRoutineParametersRequest(
    int? IntervalKm,
    int? IntervalMonths,
    decimal? DueSoonThresholdPercent,
    string? UpdatedBy);
