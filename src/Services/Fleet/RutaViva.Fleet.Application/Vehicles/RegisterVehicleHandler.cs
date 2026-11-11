using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class RegisterVehicleHandler(
    IVehicleRepository vehicles,
    IVehicleUniquenessChecker uniqueness,
    IFleetReferenceReader references,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RegisterVehicleCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        Result<Vehicle> registered = await RegisterAsync(command, cancellationToken);
        if (registered.IsFailure)
        {
            return registered.Error;
        }

        vehicles.Add(registered.Value);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return registered.Value.Id.Value;
    }

    private async Task<Result<Vehicle>> RegisterAsync(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;
        Result<VehicleIdentity> identity = VehicleIdentityFactory.Create(command, now.Year);
        if (identity.IsFailure)
        {
            return identity.Error;
        }

        Result<VehicleParties> parties = await VehicleReferenceResolver.ResolvePartiesAsync(references, command, cancellationToken);
        if (parties.IsFailure)
        {
            return parties.Error;
        }

        Error? conflict = await FindConflictAsync(identity.Value, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Vehicle.Register(
            identity.Value.Plate,
            identity.Value.Vin,
            identity.Value.Description,
            identity.Value.Odometer,
            parties.Value.Owner,
            parties.Value.Workshop,
            parties.Value.Responsible,
            now);
    }

    private async Task<Error?> FindConflictAsync(VehicleIdentity identity, CancellationToken cancellationToken)
    {
        if (await uniqueness.IsPlateTakenAsync(identity.Plate, cancellationToken))
        {
            return VehicleErrors.PlateAlreadyRegistered;
        }

        if (identity.Vin is not null && await uniqueness.IsVinTakenAsync(identity.Vin, cancellationToken))
        {
            return VehicleErrors.VinAlreadyRegistered;
        }

        return null;
    }
}
