using RutaViva.ServiceOrders.Domain.References;
using RutaViva.ServiceOrders.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.ServiceOrders.IntegrationTests.Api;

public sealed record ServiceOrderTestReferences(Guid VehicleId, Guid RoutineId, Guid ApproverId)
{
    private const string Plate = "TAN202";
    private const string WorkshopName = "Taller Norte";
    private const string RoutineName = "Cambio de aceite";
    private const string RoutineTypeCode = "OIL_CHANGE";
    private const string ApproverName = "Aprobador";

    public static async Task<ServiceOrderTestReferences> SeedAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid customerId = Guid.CreateVersion7();
        Guid workshopId = Guid.CreateVersion7();
        ServiceOrderTestReferences references = new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        await using ServiceOrdersDbContext context = await database.CreateDbContextAsync<ServiceOrdersDbContext>(cancellationToken);
        context.VehicleReferences.Add(VehicleReference.Create(references.VehicleId, Plate, customerId, workshopId, null, true, now));
        context.WorkshopReferences.Add(WorkshopReference.Create(workshopId, WorkshopName, now));
        context.RoutineReferences.Add(RoutineReference.Create(references.RoutineId, references.VehicleId, RoutineName, RoutineTypeCode, now));
        context.ApproverReferences.Add(ApproverReference.Create(references.ApproverId, customerId, ApproverName, true, now));
        await context.SaveChangesAsync(cancellationToken);
        return references;
    }
}
