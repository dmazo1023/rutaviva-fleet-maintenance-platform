using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Routines;

public sealed record RoutineTrackDto(
    string Kind,
    decimal ConsumedPercent,
    decimal DueSoonPercent,
    long Remaining,
    RoutineDueStatus Status)
{
    public const string DistanceKind = "km";

    public const string TimeKind = "time";
}
