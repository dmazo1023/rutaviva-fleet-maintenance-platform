using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record MaintenancePlanDetailDto(
    VehicleId VehicleId,
    string Plate,
    MaintenancePlanStatus PlanStatus,
    long OdometerKm,
    RoutineDueStatus? WorstDueStatus,
    decimal MaxConsumedPercent,
    int UpToDateCount,
    int DueSoonCount,
    int OverdueCount,
    int InactiveCount,
    IReadOnlyList<RoutineDetailDto> Routines);
