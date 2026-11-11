namespace RutaViva.IntegrationContracts.Bulk;

public sealed record RoutineBulkRow(
    int RowNumber,
    string Plate,
    string RoutineTypeCode,
    long? LastServiceKm,
    DateOnly? LastServiceDate,
    int? IntervalKm,
    int? IntervalMonths,
    int? DueSoonThresholdPercent);
