using Microsoft.EntityFrameworkCore;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Infrastructure.Persistence.Repositories;

public sealed class VehicleUniquenessChecker(FleetDbContext context) : IVehicleUniquenessChecker
{
    public async Task<bool> IsPlateTakenAsync(Plate plate, CancellationToken cancellationToken) =>
        await context.Vehicles.AsNoTracking().AnyAsync(vehicle => vehicle.Plate == plate, cancellationToken);

    public async Task<bool> IsVinTakenAsync(Vin vin, CancellationToken cancellationToken) =>
        await context.Vehicles.AsNoTracking().AnyAsync(vehicle => vehicle.Vin == vin, cancellationToken);
}
