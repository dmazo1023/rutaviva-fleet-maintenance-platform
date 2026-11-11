using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class AssignResponsibleHandler(
    IVehicleRepository vehicles,
    IFleetReferenceReader references,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<AssignResponsibleCommand, VehicleId>
{
    public async Task<Result<VehicleId>> HandleAsync(AssignResponsibleCommand command, CancellationToken cancellationToken)
    {
        Vehicle? vehicle = await vehicles.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleApplicationErrors.NotFound;
        }

        Result<ApproverReference?> responsible = await VehicleReferenceResolver.ResolveResponsibleAsync(
            references,
            command.ResponsibleApproverId,
            cancellationToken);
        if (responsible.IsFailure)
        {
            return responsible.Error;
        }

        Result assigned = vehicle.AssignResponsible(responsible.Value, clock.UtcNow);
        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.VehicleId;
    }
}
