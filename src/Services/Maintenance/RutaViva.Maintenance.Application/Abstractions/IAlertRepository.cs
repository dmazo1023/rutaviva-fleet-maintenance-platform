using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IAlertRepository
{
    Task<Alert?> GetAsync(AlertId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Alert>> GetOpenByRoutineAsync(RoutineId routineId, AlertType? type, CancellationToken cancellationToken);

    void Add(Alert alert);
}
