using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Abstractions;

public interface IMaintenancePlanQueries
{
    Task<MaintenancePlanDetailDto?> GetDetailAsync(VehicleId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenancePlanSummaryDto>> GetSummariesAsync(CustomerId? customerId, CancellationToken cancellationToken);
}
