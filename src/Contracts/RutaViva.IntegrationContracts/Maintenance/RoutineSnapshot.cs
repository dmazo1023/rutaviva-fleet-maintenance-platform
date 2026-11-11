namespace RutaViva.IntegrationContracts.Maintenance;

public sealed record RoutineSnapshot(
    Guid RoutineId,
    Guid VehicleId,
    string Plate,
    Guid CustomerId,
    string RoutineTypeCode,
    string RoutineName,
    string State,
    string DueStatus,
    int CycleNumber,
    long CycleStartKm,
    DateOnly CycleStartDate,
    int? IntervalKm,
    int? IntervalMonths,
    int EffectiveDueSoonPercent,
    long? NextDueKm,
    DateOnly? NextDueDate,
    long OdometerKm,
    decimal ConsumedPercent);
