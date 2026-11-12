using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record NextMaintenanceDto(
    string RoutineName,
    string Kind,
    long Remaining,
    RoutineDueStatus DueStatus,
    AlertTrigger? TriggeredBy);
