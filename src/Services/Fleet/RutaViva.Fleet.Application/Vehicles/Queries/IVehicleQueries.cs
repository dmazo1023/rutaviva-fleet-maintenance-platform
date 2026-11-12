using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public interface IVehicleQueries
{
    Task<PagedResult<VehicleSummaryDto>> ListAsync(VehicleListCriteria criteria, CancellationToken cancellationToken);

    Task<VehicleDetailDto?> GetDetailAsync(VehicleId id, CancellationToken cancellationToken);

    Task<VehicleDetailDto?> GetByPlateAsync(Plate plate, CancellationToken cancellationToken);
}
