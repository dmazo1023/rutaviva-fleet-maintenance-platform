namespace RutaViva.Maintenance.Application.Evaluations;

public sealed record ReevaluationSummary(int EvaluatedPlans, int DueSoon, int Overdue, int OrdersRequested);
