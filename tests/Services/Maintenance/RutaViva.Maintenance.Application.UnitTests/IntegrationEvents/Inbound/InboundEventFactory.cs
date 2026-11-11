using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.IntegrationContracts.ServiceOrders;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Inbound;

internal static class InboundEventFactory
{
    public const string ActiveStatus = "Active";
    public const string InactiveStatus = "Inactive";
    public const string PlateValue = "ABC123";
    public const long OdometerKm = 52300;
    public const long ServicedKm = 52800;

    private const string BrandValue = "Brand";
    private const string ModelValue = "Model";
    private const int ModelYearValue = 2024;
    private const string OrderNumber = "SO-0001";
    private const string OrderOrigin = "Maintenance";
    private const string CompletedStatus = "Completed";
    private const string MetadataType = "Test";
    private const string MetadataSubject = "test.subject";
    private const long MetadataOffset = 1;
    private const int Year = 2026;
    private const int Month = 3;
    private const int Day = 10;
    private const int Hour = 10;

    public static DateTimeOffset Instant() => new(Year, Month, Day, Hour, 0, 0, TimeSpan.Zero);

    public static EventMetadata Metadata() => new(Guid.NewGuid(), MetadataOffset, MetadataType, MetadataSubject, Instant());

    public static VehicleRegisteredV1 VehicleRegistered(Guid vehicleId, string status) =>
        new(
            vehicleId,
            PlateValue,
            null,
            BrandValue,
            ModelValue,
            ModelYearValue,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            status,
            OdometerKm,
            Instant(),
            Instant());

    public static VehicleStatusChangedV1 VehicleStatusChanged(Guid vehicleId, string status) =>
        new(vehicleId, PlateValue, Guid.NewGuid(), status, Instant());

    public static ServiceOrderCompletedV1 ServiceOrderCompleted(Guid vehicleId, Guid? routineId) =>
        new(Order(vehicleId, routineId), ServicedKm, Instant(), Instant());

    private static ServiceOrderSnapshot Order(Guid vehicleId, Guid? routineId) =>
        new(
            Guid.NewGuid(),
            OrderNumber,
            OrderOrigin,
            vehicleId,
            PlateValue,
            routineId,
            null,
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
            Instant());
}
