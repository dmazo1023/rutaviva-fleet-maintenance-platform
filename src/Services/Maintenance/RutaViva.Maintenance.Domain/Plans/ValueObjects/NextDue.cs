using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.Maintenance.Domain.Plans.ValueObjects;

public sealed record NextDue : ValueObject
{
    private NextDue(long? kilometers, DateOnly? date)
    {
        Kilometers = kilometers;
        Date = date;
    }

    public long? Kilometers { get; }

    public DateOnly? Date { get; }

    public static NextDue Calculate(MaintenanceCycle cycle, MaintenanceInterval interval) =>
        new(KilometersOf(cycle, interval), DateOf(cycle, interval));

    private static long? KilometersOf(MaintenanceCycle cycle, MaintenanceInterval interval)
    {
        if (interval.Kilometers is not { } kilometers)
        {
            return null;
        }

        return cycle.StartKm + kilometers;
    }

    private static DateOnly? DateOf(MaintenanceCycle cycle, MaintenanceInterval interval)
    {
        if (interval.Months is not { } months)
        {
            return null;
        }

        return cycle.StartDate.AddMonths(months);
    }
}
