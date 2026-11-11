using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken);

    void Add(Vehicle vehicle);
}
