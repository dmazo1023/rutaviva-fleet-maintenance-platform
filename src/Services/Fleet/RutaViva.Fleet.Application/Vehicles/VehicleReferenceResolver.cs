using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;

namespace RutaViva.Fleet.Application.Vehicles;

internal static class VehicleReferenceResolver
{
    public static async Task<Result<VehicleParties>> ResolvePartiesAsync(
        IFleetReferenceReader references,
        RegisterVehicleCommand command,
        CancellationToken cancellationToken)
    {
        CustomerReference? owner = await references.FindCustomerAsync(command.CustomerId, cancellationToken);
        if (owner is null)
        {
            return VehicleErrors.OwnerNotFound;
        }

        WorkshopReference? workshop = await references.FindWorkshopAsync(command.WorkshopId, cancellationToken);
        if (workshop is null)
        {
            return VehicleErrors.WorkshopNotFound;
        }

        Result<ApproverReference?> responsible = await ResolveResponsibleAsync(references, command.ResponsibleApproverId, cancellationToken);
        if (responsible.IsFailure)
        {
            return responsible.Error;
        }

        return new VehicleParties(owner, workshop, responsible.Value);
    }

    public static async Task<Result<ApproverReference?>> ResolveResponsibleAsync(
        IFleetReferenceReader references,
        ApproverId? responsibleId,
        CancellationToken cancellationToken)
    {
        if (responsibleId is null)
        {
            return Result<ApproverReference?>.Success(null);
        }

        ApproverReference? approver = await references.FindApproverAsync(responsibleId.Value, cancellationToken);
        if (approver is null)
        {
            return VehicleErrors.ResponsibleNotFound;
        }

        return Result<ApproverReference?>.Success(approver);
    }
}
