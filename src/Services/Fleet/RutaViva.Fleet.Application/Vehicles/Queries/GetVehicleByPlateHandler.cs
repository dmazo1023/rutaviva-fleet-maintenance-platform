using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class GetVehicleByPlateHandler(IVehicleQueries vehicles) : IQueryHandler<GetVehicleByPlateQuery, VehicleDetailDto>
{
    public async Task<Result<VehicleDetailDto>> HandleAsync(GetVehicleByPlateQuery query, CancellationToken cancellationToken)
    {
        Result<Plate> plate = Plate.Create(query.Plate);
        if (plate.IsFailure)
        {
            return plate.Error;
        }

        VehicleDetailDto? detail = await vehicles.GetByPlateAsync(plate.Value, cancellationToken);
        if (detail is null)
        {
            return VehicleApplicationErrors.NotFound;
        }

        return detail;
    }
}
