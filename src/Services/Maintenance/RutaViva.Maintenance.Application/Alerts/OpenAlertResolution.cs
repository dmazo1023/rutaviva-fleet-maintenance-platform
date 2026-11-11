using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Alerts;

internal static class OpenAlertResolution
{
    public static async Task ResolveAsync(
        IAlertRepository alerts,
        RoutineId routineId,
        AlertType? type,
        AlertResolutionReason reason,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Alert> openAlerts = await alerts.GetOpenByRoutineAsync(routineId, type, cancellationToken);
        foreach (Alert alert in openAlerts)
        {
            alert.Resolve(reason, at);
        }
    }
}
