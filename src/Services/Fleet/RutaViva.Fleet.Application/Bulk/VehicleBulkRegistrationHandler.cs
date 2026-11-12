using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Fleet.Application.Bulk;

public sealed class VehicleBulkRegistrationHandler(
    ICommandHandler<RegisterVehicleCommand, Guid> commandHandler,
    IFleetReferenceReader references,
    IVehicleUniquenessChecker uniqueness)
    : BulkRegistrationHandler<VehicleBulkRow, RegisterVehicleCommand>(commandHandler)
{
    private const long DefaultInitialOdometerKm = 0;
    private const string BlankPlateKeyPrefix = "row:";

    protected override int RowNumberOf(VehicleBulkRow row) => row.RowNumber;

    protected override string NaturalKey(VehicleBulkRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Plate))
        {
            return BlankPlateKeyPrefix + row.RowNumber;
        }

        return row.Plate.Trim().ToUpperInvariant();
    }

    protected override async Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken)
    {
        Result<Plate> plate = Plate.Create(naturalKey);
        if (plate.IsFailure)
        {
            return false;
        }

        return await uniqueness.IsPlateTakenAsync(plate.Value, cancellationToken);
    }

    protected override async Task<Result<RegisterVehicleCommand>> ResolveCommandAsync(
        VehicleBulkRow row,
        CancellationToken cancellationToken)
    {
        CustomerReference? owner = await references.FindCustomerByTaxIdAsync(row.OwnerTaxId, cancellationToken);
        if (owner is null)
        {
            return VehicleErrors.OwnerNotFound;
        }

        WorkshopReference? workshop = await references.FindWorkshopByTaxIdAsync(row.WorkshopTaxId, cancellationToken);
        if (workshop is null)
        {
            return VehicleErrors.WorkshopNotFound;
        }

        Result<ApproverId?> responsible = await ResolveResponsibleAsync(row.ResponsibleApproverEmail, cancellationToken);
        if (responsible.IsFailure)
        {
            return responsible.Error;
        }

        return ToCommand(row, owner.Id, workshop.Id, responsible.Value);
    }

    private static RegisterVehicleCommand ToCommand(
        VehicleBulkRow row,
        CustomerId customerId,
        WorkshopId workshopId,
        ApproverId? responsibleId) =>
        new(
            row.Plate,
            row.Vin,
            row.Brand,
            row.Model,
            row.ModelYear,
            row.InitialOdometerKm ?? DefaultInitialOdometerKm,
            customerId,
            workshopId,
            responsibleId);

    private async Task<Result<ApproverId?>> ResolveResponsibleAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<ApproverId?>.Success(null);
        }

        IReadOnlyList<ApproverReference> approvers = await references.FindApproversByEmailAsync(email.Trim(), cancellationToken);
        if (approvers.Count == 0)
        {
            return VehicleErrors.ResponsibleNotFound;
        }

        if (approvers.Count > 1)
        {
            return VehicleErrors.ResponsibleAmbiguous;
        }

        return Result<ApproverId?>.Success(approvers[0].Id);
    }
}
