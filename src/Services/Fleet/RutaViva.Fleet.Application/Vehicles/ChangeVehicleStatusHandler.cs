using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class ChangeVehicleStatusHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ChangeVehicleStatusCommand, VehicleId>
{
    private static readonly IReadOnlyDictionary<VehicleStatus, Func<Vehicle, DateTimeOffset, Result>> Transitions =
        new Dictionary<VehicleStatus, Func<Vehicle, DateTimeOffset, Result>>
        {
            [VehicleStatus.Active] = static (vehicle, now) => vehicle.Reactivate(now),
            [VehicleStatus.Inactive] = static (vehicle, now) => vehicle.Deactivate(now)
        };

    public async Task<Result<VehicleId>> HandleAsync(ChangeVehicleStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Transitions.TryGetValue(command.Target, out Func<Vehicle, DateTimeOffset, Result>? transition))
        {
            return VehicleApplicationErrors.InvalidStatus;
        }

        Vehicle? vehicle = await vehicles.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleApplicationErrors.NotFound;
        }

        Result changed = transition(vehicle, clock.UtcNow);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.VehicleId;
    }
}
