using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class GetVehicleDetailHandler(IVehicleQueries vehicles) : IQueryHandler<GetVehicleDetailQuery, VehicleDetailDto>
{
    public async Task<Result<VehicleDetailDto>> HandleAsync(GetVehicleDetailQuery query, CancellationToken cancellationToken)
    {
        VehicleDetailDto? detail = await vehicles.GetDetailAsync(query.VehicleId, cancellationToken);
        if (detail is null)
        {
            return VehicleApplicationErrors.NotFound;
        }

        return detail;
    }
}
