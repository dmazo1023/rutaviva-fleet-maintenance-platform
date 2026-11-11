namespace RutaViva.Maintenance.Domain.Plans.Evaluation;

internal static class DueStatusClassifier
{
    internal const decimal FullPercent = 100m;

    private const long OverdueRemainingLimit = 0;

    internal static RoutineDueStatus Classify(long remaining, decimal dueSoonWindow)
    {
        if (remaining <= OverdueRemainingLimit)
        {
            return RoutineDueStatus.Overdue;
        }

        if (remaining <= dueSoonWindow)
        {
            return RoutineDueStatus.DueSoon;
        }

        return RoutineDueStatus.UpToDate;
    }
}
