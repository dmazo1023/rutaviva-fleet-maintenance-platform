using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class ServiceOrderReferenceResolver(IServiceOrderReferenceStore references)
{
    public async Task<Result<ServiceOrderReference>> ResolveAsync(Guid vehicleId, Guid routineId, CancellationToken cancellationToken)
    {
        VehicleReference? vehicle = await references.FindVehicleAsync(vehicleId, cancellationToken);
        if (vehicle is not { IsActive: true })
        {
            return ServiceOrderErrors.VehicleNotFound;
        }

        RoutineReference? routine = await references.FindRoutineAsync(routineId, cancellationToken);
        if (routine is null || routine.VehicleId != vehicleId)
        {
            return ServiceOrderErrors.RoutineNotFound;
        }

        WorkshopReference? workshop = await references.FindWorkshopAsync(vehicle.WorkshopId, cancellationToken);
        if (workshop is null)
        {
            return ServiceOrderErrors.WorkshopNotFound;
        }

        return new ServiceOrderReference(
            vehicle.Id,
            vehicle.Plate,
            vehicle.CustomerId,
            workshop.Id,
            workshop.Name,
            routine.Id,
            routine.RoutineName,
            routine.RoutineTypeCode,
            vehicle.ResponsibleApproverId);
    }
}
