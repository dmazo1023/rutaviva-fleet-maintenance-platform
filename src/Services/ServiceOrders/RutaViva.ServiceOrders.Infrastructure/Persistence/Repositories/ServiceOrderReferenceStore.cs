using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrderReferenceStore(ServiceOrdersDbContext context) : IServiceOrderReferenceStore
{
    public async Task<VehicleReference?> FindVehicleAsync(Guid id, CancellationToken cancellationToken) =>
        await context.VehicleReferences.FindAsync([id], cancellationToken);

    public async Task<WorkshopReference?> FindWorkshopAsync(Guid id, CancellationToken cancellationToken) =>
        await context.WorkshopReferences.FindAsync([id], cancellationToken);

    public async Task<RoutineReference?> FindRoutineAsync(Guid id, CancellationToken cancellationToken) =>
        await context.RoutineReferences.FindAsync([id], cancellationToken);

    public async Task<ApproverReference?> FindApproverAsync(Guid id, CancellationToken cancellationToken) =>
        await context.ApproverReferences.FindAsync([id], cancellationToken);

    public void AddVehicle(VehicleReference reference) => context.VehicleReferences.Add(reference);

    public void AddWorkshop(WorkshopReference reference) => context.WorkshopReferences.Add(reference);

    public void AddRoutine(RoutineReference reference) => context.RoutineReferences.Add(reference);

    public void AddApprover(ApproverReference reference) => context.ApproverReferences.Add(reference);
}
