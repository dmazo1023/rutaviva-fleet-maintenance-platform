using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.Abstractions;

public interface IServiceOrderReferenceStore
{
    Task<VehicleReference?> FindVehicleAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkshopReference?> FindWorkshopAsync(Guid id, CancellationToken cancellationToken);

    Task<RoutineReference?> FindRoutineAsync(Guid id, CancellationToken cancellationToken);

    Task<ApproverReference?> FindApproverAsync(Guid id, CancellationToken cancellationToken);

    void AddVehicle(VehicleReference reference);

    void AddWorkshop(WorkshopReference reference);

    void AddRoutine(RoutineReference reference);

    void AddApprover(ApproverReference reference);
}
