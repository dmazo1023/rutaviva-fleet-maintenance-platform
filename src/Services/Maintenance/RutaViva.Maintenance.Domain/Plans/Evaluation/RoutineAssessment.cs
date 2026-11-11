using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public sealed record RoutineAssessment(
    RoutineDueStatus Status,
    AlertTrigger Trigger,
    decimal ConsumedPercent,
    NextDue? NextDue);
