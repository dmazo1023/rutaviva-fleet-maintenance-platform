using Microsoft.EntityFrameworkCore;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(FleetDbContext context) : IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken) =>
        await context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public void Add(Vehicle vehicle) => context.Vehicles.Add(vehicle);
}
