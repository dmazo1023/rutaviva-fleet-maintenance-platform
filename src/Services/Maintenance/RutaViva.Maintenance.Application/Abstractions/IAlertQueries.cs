using RutaViva.Maintenance.Application.Alerts;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IAlertQueries
{
    Task<PagedResult<AlertDto>> SearchAsync(AlertSearchCriteria criteria, CancellationToken cancellationToken);
}
