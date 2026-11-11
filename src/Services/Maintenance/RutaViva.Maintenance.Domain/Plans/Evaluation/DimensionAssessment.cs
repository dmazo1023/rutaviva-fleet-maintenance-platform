namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

public sealed record DimensionAssessment(
    DueDimension Dimension,
    RoutineDueStatus Status,
    decimal ConsumedPercent,
    long Remaining,
    decimal DueSoonWindowPercent);
