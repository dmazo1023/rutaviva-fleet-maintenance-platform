using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.IntegrationTests.Persistence;

internal sealed class PassingUniquenessChecker : IVehicleUniquenessChecker
{
    public Task<bool> IsPlateTakenAsync(Plate plate, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> IsVinTakenAsync(Vin vin, CancellationToken cancellationToken) => Task.FromResult(false);
}
