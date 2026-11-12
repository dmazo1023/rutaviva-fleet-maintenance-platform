using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Fleet.Application.Bulk;

public sealed class RegisterVehiclesBulkHandler(
    IVehicleRepository vehicles,
    IVehicleUniquenessChecker uniqueness,
    IFleetReferenceReader references,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEnumerable<IValidator<RegisterVehicleCommand>> validators)
    : ICommandHandler<RegisterVehiclesBulkCommand, IReadOnlyList<BulkRowResult>>
{
    public async Task<Result<IReadOnlyList<BulkRowResult>>> HandleAsync(
        RegisterVehiclesBulkCommand command,
        CancellationToken cancellationToken)
    {
        VehicleBulkRegistrationHandler registration = CreateRegistration(command.DryRun);
        IReadOnlyList<BulkRowResult> results = await registration.ExecuteAsync(command.Rows, cancellationToken);
        return Result<IReadOnlyList<BulkRowResult>>.Success(results);
    }

    private VehicleBulkRegistrationHandler CreateRegistration(bool dryRun)
    {
        IUnitOfWork scopedUnitOfWork = BulkExecutionScope.For(dryRun).Resolve(unitOfWork);
        RegisterVehicleHandler registerVehicle = new(vehicles, uniqueness, references, scopedUnitOfWork, clock);
        ValidationCommandHandlerDecorator<RegisterVehicleCommand, Guid> validated = new(registerVehicle, validators);
        return new VehicleBulkRegistrationHandler(validated, references, uniqueness);
    }
}
