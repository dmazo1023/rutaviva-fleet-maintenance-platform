using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Repositories;

public sealed class AlertRepository(MaintenanceDbContext context) : IAlertRepository
{
    public async Task<Alert?> GetAsync(AlertId id, CancellationToken cancellationToken) =>
        await context.Alerts.FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Alert>> GetOpenByRoutineAsync(
        RoutineId routineId,
        AlertType? type,
        CancellationToken cancellationToken)
    {
        IQueryable<Alert> open = context.Alerts
            .Where(alert => alert.RoutineId == routineId && alert.Status != AlertStatus.Resolved);
        if (type is null)
        {
            return await open.ToListAsync(cancellationToken);
        }

        return await open.Where(alert => alert.Type == type).ToListAsync(cancellationToken);
    }

    public void Add(Alert alert) => context.Alerts.Add(alert);
}
