using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal static class NextMaintenanceSelector
{
    internal static NextMaintenanceDto? Select(IReadOnlyList<RoutineDetailDto> routines)
    {
        RoutineDetailDto? routine = routines
            .Where(candidate => candidate.State == RoutineState.Active)
            .OrderByDescending(candidate => candidate.ConsumedPercent)
            .FirstOrDefault();
        RoutineTrackDto? track = routine is null ? null : MostConsumedTrack(routine);

        return routine is null || track is null
            ? null
            : new NextMaintenanceDto(routine.Name, track.Kind, track.Remaining, routine.DueStatus, routine.TriggeredBy);
    }

    private static RoutineTrackDto? MostConsumedTrack(RoutineDetailDto routine) =>
        new[] { routine.DistanceTrack, routine.TimeTrack }
            .OfType<RoutineTrackDto>()
            .OrderByDescending(track => track.ConsumedPercent)
            .FirstOrDefault();
}
