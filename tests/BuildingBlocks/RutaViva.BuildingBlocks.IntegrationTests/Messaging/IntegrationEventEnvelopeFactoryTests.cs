using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Messaging;

public sealed class IntegrationEventEnvelopeFactoryTests
{
    private const string ServiceName = "fleet";
    private const string ExpectedType = "rutaviva.fleet.VehicleRegistered.v1";
    private const string ExpectedSource = "/rutaviva/fleet";
    private const string ExpectedStream = "fleet.vehicle";
    private const string Subject = "vehicle-subject";
    private const string Plate = "ABC123";
    private const string Brand = "Brand";
    private const string Model = "Model";
    private const string Status = "Active";
    private const int ModelYear = 2024;
    private const long InitialOdometerKm = 1000;

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_UseDescriptorType_When_EventIsVehicleRegisteredV1()
    {
        IntegrationEventEnvelopeFactory factory = CreateFactory();

        IntegrationEventEnvelope envelope = factory.Create(CreateEvent(), Subject);

        envelope.Type.Should().Be(ExpectedType);
        envelope.Source.Should().Be(ExpectedSource);
        envelope.Time.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_UseDescriptorStream_When_OutboxMessageIsBuilt()
    {
        OutboxMessageFactory factory = new(CreateFactory());

        OutboxMessage message = factory.Create(CreateEvent(), Subject);

        message.Stream.Should().Be(ExpectedStream);
        message.EventType.Should().Be(ExpectedType);
    }

    private static IntegrationEventEnvelopeFactory CreateFactory()
    {
        IntegrationEventSettings settings = new() { ServiceName = ServiceName };
        return new IntegrationEventEnvelopeFactory(new FakeClock(Now), settings);
    }

    private static VehicleRegisteredV1 CreateEvent() => new(
        Guid.NewGuid(),
        Plate,
        null,
        Brand,
        Model,
        ModelYear,
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        Status,
        InitialOdometerKm,
        Now,
        Now);
}
