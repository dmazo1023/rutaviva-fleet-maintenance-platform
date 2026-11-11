using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Alerts;

public sealed record AlertOrigin(
    VehicleId VehicleId,
    LicensePlate Plate,
    CustomerId CustomerId,
    RoutineId RoutineId,
    string RoutineName,
    int CycleNumber);
