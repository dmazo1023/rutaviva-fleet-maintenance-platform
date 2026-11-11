using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.RoutineTypes;

public sealed record RoutineTypeParameters(
    RoutineTypeCode Code,
    string Name,
    MaintenanceInterval Interval,
    DueSoonThreshold Threshold);
