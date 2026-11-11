using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;
using RutaViva.Testing.Fixtures;

namespace RutaViva.EventBus.Hub.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class EventsHubTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const int PageSize = 10;
    private const char StreamFiller = 'a';

    [Fact]
    public async Task Publish_Should_NotifyOnlySubscribersOfTheStream_When_EventIsPublished()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusHubFactory factory = await CreateFactoryAsync(cancellationToken);
        await using HubSubscriber fleet = await HubSubscriber.ConnectAsync(factory, Streams.FleetVehicle, cancellationToken);
        await using HubSubscriber alerts = await HubSubscriber.ConnectAsync(factory, Streams.MaintenanceAlert, cancellationToken);

        long fleetOffset = await PublishAsync(fleet, Streams.FleetVehicle, cancellationToken);
        long alertOffset = await PublishAsync(fleet, Streams.MaintenanceAlert, cancellationToken);

        Notification fleetNotification = await fleet.NextAsync(cancellationToken);
        Notification alertNotification = await alerts.NextAsync(cancellationToken);
        fleetNotification.Should().Be(new Notification(Streams.FleetVehicle, fleetOffset));
        alertNotification.Should().Be(new Notification(Streams.MaintenanceAlert, alertOffset));
    }

    [Fact]
    public async Task GetEvents_Should_ReturnPublishedEvent_When_EventWasPublished()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusHubFactory factory = await CreateFactoryAsync(cancellationToken);
        await using HubSubscriber subscriber = await HubSubscriber.ConnectAsync(factory, Streams.FleetVehicle, cancellationToken);
        IntegrationEventEnvelope envelope = TestEnvelopes.Create();
        long offset = await subscriber.Connection.InvokeAsync<long>(EventBusHubMethods.Publish, Streams.FleetVehicle, envelope, cancellationToken);
        string[] streams = [Streams.FleetVehicle];

        List<EventRecord> records = await subscriber.Connection.InvokeAsync<List<EventRecord>>(EventBusHubMethods.GetEvents, streams, offset - 1, PageSize, cancellationToken);

        EventRecord record = records.Should().ContainSingle().Subject;
        record.Offset.Should().Be(offset);
        record.Envelope.Id.Should().Be(envelope.Id);
    }

    [Fact]
    public async Task Publish_Should_ThrowHubException_When_StreamExceedsMaxLength()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusHubFactory factory = await CreateFactoryAsync(cancellationToken);
        await using HubSubscriber subscriber = await HubSubscriber.ConnectAsync(factory, Streams.FleetVehicle, cancellationToken);
        string stream = new(StreamFiller, IntegrationEventLimits.StreamMaxLength + 1);

        Func<Task> publish = () => PublishAsync(subscriber, stream, cancellationToken);

        await publish.Should().ThrowAsync<HubException>();
    }

    private static Task<long> PublishAsync(HubSubscriber publisher, string stream, CancellationToken cancellationToken) =>
        publisher.Connection.InvokeAsync<long>(EventBusHubMethods.Publish, stream, TestEnvelopes.Create(), cancellationToken);

    private async Task<EventBusHubFactory> CreateFactoryAsync(CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await database.CreateDbContextAsync<EventBusDbContext>(cancellationToken);
        return new EventBusHubFactory(database.ConnectionString);
    }
}
