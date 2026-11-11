using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal sealed record PlanReadRow(
    VehicleId VehicleId,
    LicensePlate Plate,
    MaintenancePlanStatus Status,
    long OdometerKm,
    DateOnly LastTimeEvaluationDate);
