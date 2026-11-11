using RutaViva.IntegrationContracts.ServiceOrders;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal static class ServiceOrderCompletedFactory
{
    private const string OrderNumber = "SO-0001";
    private const string AutomaticOrigin = "Automatic";
    private const string RoutineName = "Brake service";
    private const string CompletedStatus = "Completed";

    public static ServiceOrderCompletedV1 Create(
        Guid serviceOrderId,
        Guid vehicleId,
        string plate,
        Guid routineId,
        long servicedKm,
        DateTimeOffset servicedAt)
    {
        ServiceOrderSnapshot order = new(
            serviceOrderId,
            OrderNumber,
            AutomaticOrigin,
            vehicleId,
            plate,
            routineId,
            RoutineName,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CompletedStatus,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            servicedAt);
        return new ServiceOrderCompletedV1(order, servicedKm, servicedAt, servicedAt);
    }
}
