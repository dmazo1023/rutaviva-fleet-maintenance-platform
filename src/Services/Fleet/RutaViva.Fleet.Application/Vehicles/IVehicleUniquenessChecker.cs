using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles;

public interface IVehicleUniquenessChecker
{
    Task<bool> IsPlateTakenAsync(Plate plate, CancellationToken cancellationToken);

    Task<bool> IsVinTakenAsync(Vin vin, CancellationToken cancellationToken);
}
