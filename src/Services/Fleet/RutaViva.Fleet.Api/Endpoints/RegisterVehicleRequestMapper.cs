using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Api.Contracts;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Api.Endpoints;

internal static class RegisterVehicleRequestMapper
{
    public static Result<RegisterVehicleCommand> ToCommand(RegisterVehicleRequest request)
    {
        Result<CustomerId> customer = CustomerId.Create(request.CustomerId);
        if (customer.IsFailure)
        {
            return customer.Error;
        }

        Result<WorkshopId> workshop = WorkshopId.Create(request.WorkshopId);
        if (workshop.IsFailure)
        {
            return workshop.Error;
        }

        Result<ApproverId?> responsible = OptionalIdentifiers.Approver(request.ResponsibleApproverId);
        if (responsible.IsFailure)
        {
            return responsible.Error;
        }

        return new RegisterVehicleCommand(
            request.Plate ?? string.Empty,
            request.Vin,
            request.Brand ?? string.Empty,
            request.Model ?? string.Empty,
            request.ModelYear,
            request.InitialOdometerKm,
            customer.Value,
            workshop.Value,
            responsible.Value);
    }
}
