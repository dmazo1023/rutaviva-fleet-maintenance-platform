using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Plans.Queries;

public sealed class GetMaintenancePlanHandler(IMaintenancePlanQueries plans)
    : IQueryHandler<GetMaintenancePlanQuery, MaintenancePlanDetailDto>
{
    public async Task<Result<MaintenancePlanDetailDto>> HandleAsync(GetMaintenancePlanQuery query, CancellationToken cancellationToken)
    {
        MaintenancePlanDetailDto? detail = await plans.GetDetailAsync(query.VehicleId, cancellationToken);
        if (detail is null)
        {
            return MaintenanceErrors.MaintenancePlanNotFound;
        }

        return detail;
    }
}
