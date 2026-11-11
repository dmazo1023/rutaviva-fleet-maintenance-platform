using System.Text.Json;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class RestartIdempotencyTests
{
    private const string Plate = "RVA104";
    private const string Brakes = "BRAKES";
    private const string InactiveStatus = "Inactive";
    private const string SuspendedStatus = "Suspended";
    private const int RedeliveryCount = 5;
    private const int StatusRedeliveryCount = 3;
    private const int ExpectedSecondCycle = 2;
    private const int SingleEvent = 1;
    private const long ServicedKm = 98400;

    private static readonly string RestartedType = IntegrationEventDescriptor.For<MaintenanceRoutineRestartedV1>().EventType.ToString();
    private static readonly string PlanStatusChangedType = IntegrationEventDescriptor.For<MaintenancePlanStatusChangedV1>().EventType.ToString();

    [Fact]
    public async Task HandleAsync_Should_RestartRoutineOnce_When_ServiceOrderCompletedIsDeliveredFiveTimes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await StartWithPlateAsync(cancellationToken);
        Guid vehicleId = harness.VehicleIdOf(Plate);
        JsonElement initial = await harness.Api.GetPlanAsync(vehicleId, cancellationToken);
        ServiceOrderCompletedV1 completed = ServiceOrderCompletedFactory.Create(
            Guid.NewGuid(),
            vehicleId,
            Plate,
            ScenarioJson.RoutineIdOf(initial, Brakes),
            ServicedKm,
            harness.Clock.UtcNow);

        await DeliverRepeatedlyAsync(harness, completed, cancellationToken);

        JsonElement plan = await harness.Api.GetPlanAsync(vehicleId, cancellationToken);
        IReadOnlyList<string> restarts = await harness.ReadOutboxTypesAsync(Plate, [RestartedType], cancellationToken);
        ScenarioJson.CycleNumberOf(plan, Brakes).Should().Be(ExpectedSecondCycle);
        ScenarioJson.CycleStartKmOf(plan, Brakes).Should().Be(ServicedKm);
        restarts.Should().HaveCount(SingleEvent);
    }

    [Fact]
    public async Task HandleAsync_Should_SuspendPlanOnce_When_VehicleInactiveIsDeliveredThreeTimes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await StartWithPlateAsync(cancellationToken);
        Guid vehicleId = harness.VehicleIdOf(Plate);
        VehicleStatusChangedV1 inactive = new(vehicleId, Plate, Guid.NewGuid(), InactiveStatus, harness.Clock.UtcNow);
        Guid messageId = Guid.NewGuid();
        long offset = harness.NextOffset();

        for (int delivery = 0; delivery < StatusRedeliveryCount; delivery++)
        {
            await harness.DeliverAsync<VehicleStatusChangedV1, VehicleStatusChangedHandler>(inactive, messageId, offset, cancellationToken);
        }

        JsonElement plan = await harness.Api.GetPlanAsync(vehicleId, cancellationToken);
        IReadOnlyList<string> changes = await harness.ReadOutboxTypesAsync(Plate, [PlanStatusChangedType], cancellationToken);
        ScenarioJson.PlanStatusOf(plan).Should().Be(SuspendedStatus);
        changes.Should().HaveCount(SingleEvent);
    }

    private static async Task<DemoScenarioHarness> StartWithPlateAsync(CancellationToken cancellationToken)
    {
        DemoScenarioHarness harness = await DemoScenarioHarness.StartAsync(cancellationToken);
        await harness.RegisterVehiclesAsync(SampleCsv.Vehicles().Where(vehicle => vehicle.Plate == Plate), cancellationToken);
        await harness.LoadRoutinesAsync(SampleCsv.Routines().Where(row => row.Plate == Plate).ToList(), cancellationToken);
        return harness;
    }

    private static async Task DeliverRepeatedlyAsync(
        DemoScenarioHarness harness,
        ServiceOrderCompletedV1 completed,
        CancellationToken cancellationToken)
    {
        Guid messageId = Guid.NewGuid();
        long offset = harness.NextOffset();
        for (int delivery = 0; delivery < RedeliveryCount; delivery++)
        {
            await harness.DeliverAsync<ServiceOrderCompletedV1, ServiceOrderCompletedHandler>(completed, messageId, offset, cancellationToken);
        }
    }
}
