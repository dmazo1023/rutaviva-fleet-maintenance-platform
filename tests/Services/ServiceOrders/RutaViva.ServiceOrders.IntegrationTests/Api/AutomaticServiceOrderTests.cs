using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;
using RutaViva.Testing.Fixtures;

namespace RutaViva.ServiceOrders.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class AutomaticServiceOrderTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string VehicleOrdersFormat = "/api/v1/service-orders?vehicleId={0}";
    private const string OriginProperty = "origin";
    private const string StatusProperty = "status";
    private const string Automatic = "Automatic";
    private const string Created = "Created";
    private const string EventType = "rutaviva.maintenance.MaintenanceRoutineOverdue.v1";
    private const string Plate = "TAN202";
    private const string RoutineTypeCode = "OIL_CHANGE";
    private const string RoutineName = "Cambio de aceite";
    private const string ActiveState = "Active";
    private const string OverdueStatus = "Overdue";
    private const string Distance = "Distance";
    private const int CycleNumber = 1;
    private const long CycleStartKm = 10_000;
    private const int IntervalKm = 5_000;
    private const int DueSoonPercent = 10;
    private const long OdometerKm = 15_100;
    private const decimal ConsumedPercent = 102m;

    [Fact]
    public async Task OverdueEvent_Should_CreateOneAutomaticOrder_When_ReceivedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceOrdersApiFactory factory = await ServiceOrdersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        ServiceOrderTestReferences references = await ServiceOrderTestReferences.SeedAsync(database, cancellationToken);
        MaintenanceRoutineOverdueV1 overdue = Overdue(references);

        Result first = await HandleAsync(factory, overdue, cancellationToken);
        Result second = await HandleAsync(factory, overdue, cancellationToken);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        JsonElement orders = await client.GetFromJsonAsync<JsonElement>(string.Format(VehicleOrdersFormat, references.VehicleId), cancellationToken);
        orders.GetArrayLength().Should().Be(1);
        orders[0].GetProperty(OriginProperty).GetString().Should().Be(Automatic);
        orders[0].GetProperty(StatusProperty).GetString().Should().Be(Created);
    }

    private static async Task<Result> HandleAsync(ServiceOrdersApiFactory factory, MaintenanceRoutineOverdueV1 overdue, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        MaintenanceRoutineOverdueHandler handler = scope.ServiceProvider.GetRequiredService<MaintenanceRoutineOverdueHandler>();
        EventMetadata metadata = new(Guid.CreateVersion7(), 1, EventType, overdue.Routine.RoutineId.ToString(), overdue.OccurredAt);
        return await handler.HandleAsync(overdue, metadata, cancellationToken);
    }

    private static MaintenanceRoutineOverdueV1 Overdue(ServiceOrderTestReferences references)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RoutineSnapshot routine = new(
            references.RoutineId,
            references.VehicleId,
            Plate,
            Guid.CreateVersion7(),
            RoutineTypeCode,
            RoutineName,
            ActiveState,
            OverdueStatus,
            CycleNumber,
            CycleStartKm,
            DateOnly.FromDateTime(now.UtcDateTime),
            IntervalKm,
            null,
            DueSoonPercent,
            CycleStartKm + IntervalKm,
            null,
            OdometerKm,
            ConsumedPercent);
        return new MaintenanceRoutineOverdueV1(routine, Distance, now);
    }
}
