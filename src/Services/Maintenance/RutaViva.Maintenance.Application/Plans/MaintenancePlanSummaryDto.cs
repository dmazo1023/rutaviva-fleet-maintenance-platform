using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record MaintenancePlanSummaryDto(
    VehicleId VehicleId,
    string Plate,
    MaintenancePlanStatus PlanStatus,
    long OdometerKm,
    RoutineDueStatus? WorstDueStatus,
    decimal MaxConsumedPercent,
    int UpToDateCount,
    int DueSoonCount,
    int OverdueCount,
    int InactiveCount);
