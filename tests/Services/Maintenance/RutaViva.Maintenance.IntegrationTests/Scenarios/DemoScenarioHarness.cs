using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.IntegrationTests.Api;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal sealed class DemoScenarioHarness : IAsyncDisposable
{
    private const int ClockYear = 2026;
    private const int ClockMonth = 11;
    private const int ClockDay = 12;
    private const int ClockHour = 8;
    private const int ClockZero = 0;
    private const int BogotaOffsetHours = -5;
    private const string ActiveStatus = "Active";
    private const long FirstOffset = 1;

    private readonly SqlServerTestDatabase _database;
    private readonly MaintenanceApiFactory _rootFactory;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Dictionary<string, Guid> _vehicleIds = [];
    private readonly Dictionary<string, Guid> _partyIds = [];
    private long _offset = FirstOffset;

    private DemoScenarioHarness(
        SqlServerTestDatabase database,
        MaintenanceApiFactory rootFactory,
        WebApplicationFactory<Program> factory,
        FakeClock clock)
    {
        _database = database;
        _rootFactory = rootFactory;
        _factory = factory;
        _client = factory.CreateClient();
        Clock = clock;
        Api = new DemoApiClient(_client);
    }

    public FakeClock Clock { get; }

    public DemoApiClient Api { get; }

    public static async Task<DemoScenarioHarness> StartAsync(CancellationToken cancellationToken)
    {
        SqlServerTestDatabase database = new();
        await database.InitializeAsync();
        MaintenanceApiFactory rootFactory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        FakeClock clock = new(FixedNow());
        WebApplicationFactory<Program> factory = rootFactory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IClock>(clock))));
        return new DemoScenarioHarness(database, rootFactory, factory, clock);
    }

    public static async Task<DemoScenarioHarness> StartLoadedAsync(CancellationToken cancellationToken)
    {
        DemoScenarioHarness harness = await StartAsync(cancellationToken);
        await harness.RegisterVehiclesAsync(SampleCsv.Vehicles(), cancellationToken);
        await harness.LoadRoutinesAsync(SampleCsv.Routines(), cancellationToken);
        return harness;
    }

    public Guid VehicleIdOf(string plate)
    {
        if (_vehicleIds.TryGetValue(plate, out Guid existing))
        {
            return existing;
        }

        Guid created = Guid.NewGuid();
        _vehicleIds[plate] = created;
        return created;
    }

    public async Task RegisterVehiclesAsync(IEnumerable<SampleVehicle> vehicles, CancellationToken cancellationToken)
    {
        foreach (SampleVehicle vehicle in vehicles)
        {
            VehicleRegisteredV1 integrationEvent = ToEvent(vehicle);
            await DeliverAsync<VehicleRegisteredV1, VehicleRegisteredHandler>(integrationEvent, Guid.NewGuid(), NextOffset(), cancellationToken);
        }
    }

    public async Task<IReadOnlyList<string>> LoadRoutinesAsync(IReadOnlyList<RoutineBulkRow> rows, CancellationToken cancellationToken)
    {
        JsonElement body = await Api.LoadRoutinesAsync(rows, cancellationToken);
        return body.EnumerateArray().Select(ScenarioJson.BulkOutcomeOf).ToList();
    }

    public async Task DeliverAsync<TEvent, THandler>(TEvent integrationEvent, Guid messageId, long offset, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        THandler handler = scope.ServiceProvider.GetRequiredService<THandler>();
        string eventType = IntegrationEventDescriptor.For<TEvent>().EventType.ToString();
        EventMetadata metadata = new(messageId, offset, eventType, messageId.ToString(), Clock.UtcNow);

        Result result = await handler.HandleAsync(integrationEvent, metadata, cancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    public async Task<IReadOnlyList<string>> ReadOutboxTypesAsync(
        string payloadFragment,
        string[] eventTypes,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        MaintenanceDbContext context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
        return await context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(message => eventTypes.Contains(message.EventType) && message.Payload.Contains(payloadFragment))
            .OrderBy(message => message.Sequence)
            .Select(message => message.EventType)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _rootFactory.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static DateTimeOffset FixedNow() =>
        new(ClockYear, ClockMonth, ClockDay, ClockHour, ClockZero, ClockZero, TimeSpan.FromHours(BogotaOffsetHours));

    public long NextOffset() => _offset++;

    private VehicleRegisteredV1 ToEvent(SampleVehicle vehicle) =>
        new(
            VehicleIdOf(vehicle.Plate),
            vehicle.Plate,
            vehicle.Vin,
            vehicle.Brand,
            vehicle.Model,
            vehicle.ModelYear,
            PartyIdOf(vehicle.OwnerTaxId),
            PartyIdOf(vehicle.WorkshopTaxId),
            null,
            ActiveStatus,
            vehicle.OdometerKm,
            Clock.UtcNow,
            Clock.UtcNow);

    private Guid PartyIdOf(string taxId)
    {
        if (_partyIds.TryGetValue(taxId, out Guid existing))
        {
            return existing;
        }

        Guid created = Guid.NewGuid();
        _partyIds[taxId] = created;
        return created;
    }
}
