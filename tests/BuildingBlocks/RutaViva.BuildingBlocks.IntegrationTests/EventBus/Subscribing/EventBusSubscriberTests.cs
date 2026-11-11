using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.BuildingBlocks.EventBus.Client.Subscribing;
using RutaViva.BuildingBlocks.IntegrationTests.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

[Trait("Category", "Integration")]
public sealed class EventBusSubscriberTests(EventBusTestServer server, SqlServerTestDatabase database)
    : IClassFixture<EventBusTestServer>, IClassFixture<SqlServerTestDatabase>
{
    private const int RepublishCount = 5;
    private const int PendingEventCount = 3;
    private const int ProcessTimeoutSeconds = 30;
    private const int BackupPollSeconds = 5;
    private const int BackupPollMarginSeconds = 10;
    private const int NewerVersion = 2;
    private const string TestingService = "testing";

    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(ProcessTimeoutSeconds);
    private static readonly TimeSpan BackupPollTimeout = TimeSpan.FromSeconds(BackupPollSeconds + BackupPollMarginSeconds);

    [Fact]
    public async Task Subscriber_Should_ApplyEffectOnce_When_SameEventIsPublishedFiveTimes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.Handle<SampleHappenedV1, RecordEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await host.StartAsync(cancellationToken);
        Guid messageId = Guid.NewGuid();
        long offset = 0;

        for (int attempt = 0; attempt < RepublishCount; attempt++)
        {
            offset = await publisher.PublishAsync(stream, SampleHappenedV1.EventName, new SampleHappenedV1(Guid.NewGuid()), subject, messageId, cancellationToken);
        }

        bool processed = await host.WaitForCheckpointAsync(offset, ProcessTimeout, cancellationToken);
        List<long> offsets = await host.ReadEffectOffsetsAsync(subject, cancellationToken);
        processed.Should().BeTrue();
        offsets.Should().ContainSingle();
    }

    [Fact]
    public async Task Subscriber_Should_ProcessPendingEventsInOrder_When_StartedAfterPublishing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.Handle<SampleHappenedV1, RecordEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        List<long> published = [];
        for (int index = 0; index < PendingEventCount; index++)
        {
            published.Add(await publisher.PublishAsync(stream, SampleHappenedV1.EventName, new SampleHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken));
        }

        await host.StartAsync(cancellationToken);

        bool processed = await host.WaitForCheckpointAsync(published[^1], ProcessTimeout, cancellationToken);
        List<long> offsets = await host.ReadEffectOffsetsAsync(subject, cancellationToken);
        long checkpoint = await host.ReadCheckpointAsync(cancellationToken);
        processed.Should().BeTrue();
        offsets.Should().Equal(published);
        checkpoint.Should().Be(published[^1]);
    }

    [Fact]
    public async Task Subscriber_Should_RecoverEvent_When_NotificationIsLost()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.Handle<SampleHappenedV1, RecordEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await host.StartAsync(cancellationToken);
        bool connected = await Eventually.UntilAsync(() => Task.FromResult(host.Connection?.State == HubConnectionState.Connected), ProcessTimeout, cancellationToken);
        long warmUp = await publisher.PublishAsync(stream, SampleHappenedV1.EventName, new SampleHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);
        bool warmedUp = await host.WaitForCheckpointAsync(warmUp, ProcessTimeout, cancellationToken);
        host.Connection?.Remove(EventBusHubMethods.EventsAvailable);

        long silent = await publisher.PublishAsync(stream, SampleHappenedV1.EventName, new SampleHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        bool recovered = await host.WaitForCheckpointAsync(silent, BackupPollTimeout, cancellationToken);
        connected.Should().BeTrue();
        warmedUp.Should().BeTrue();
        recovered.Should().BeTrue();
    }

    [Fact]
    public async Task Subscriber_Should_AdvanceCheckpointWithoutDeadLetter_When_EventHasNoHandler()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.Handle<SampleHappenedV1, RecordEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await host.StartAsync(cancellationToken);

        long offset = await publisher.PublishAsync(stream, IgnoredHappenedV1.EventName, new IgnoredHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        bool advanced = await host.WaitForCheckpointAsync(offset, ProcessTimeout, cancellationToken);
        List<long> offsets = await host.ReadEffectOffsetsAsync(subject, cancellationToken);
        int deadLetters = await DeadLetterQuery.CountAsync(server.ConnectionString, host.SubscriberName, cancellationToken);
        advanced.Should().BeTrue();
        offsets.Should().BeEmpty();
        deadLetters.Should().Be(0);
    }

    [Fact]
    public async Task Subscriber_Should_DeliverBothEventsToSameHandler_When_RegisteredAsFamily()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.HandleFamily<IFamilyEvent, FamilyEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await host.StartAsync(cancellationToken);

        await publisher.PublishAsync(stream, FamilyFirstV1.EventName, new FamilyFirstV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);
        long last = await publisher.PublishAsync(stream, FamilySecondV1.EventName, new FamilySecondV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        bool processed = await host.WaitForCheckpointAsync(last, ProcessTimeout, cancellationToken);
        List<string> types = await host.ReadEffectTypesAsync(subject, cancellationToken);
        processed.Should().BeTrue();
        types.Should().Equal(TestEventPublisher.TypeFor(FamilyFirstV1.EventName), TestEventPublisher.TypeFor(FamilySecondV1.EventName));
    }

    [Fact]
    public async Task Subscriber_Should_SkipEventAndAdvanceCheckpoint_When_TypeVersionIsNotRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        string newerType = IntegrationEventType.Create(TestingService, SampleHappenedV1.EventName, NewerVersion).ToString();
        await using SubscriberTestHost host = await CreateHostAsync(builder => builder.Handle<SampleHappenedV1, RecordEffectHandler>(stream), cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await host.StartAsync(cancellationToken);

        long offset = await publisher.PublishWithTypeAsync(stream, newerType, new SampleHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        bool advanced = await host.WaitForCheckpointAsync(offset, ProcessTimeout, cancellationToken);
        List<long> offsets = await host.ReadEffectOffsetsAsync(subject, cancellationToken);
        int deadLetters = await DeadLetterQuery.CountAsync(server.ConnectionString, host.SubscriberName, cancellationToken);
        advanced.Should().BeTrue();
        offsets.Should().BeEmpty();
        deadLetters.Should().Be(0);
    }

    [Fact]
    public async Task Subscriber_Should_Throw_When_SameEventTypeIsRegisteredTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();

        Func<Task> create = () => CreateHostAsync(
            builder => builder
                .Handle<SampleHappenedV1, RecordEffectHandler>(stream)
                .Handle<SampleHappenedV1, RecordEffectHandler>(stream),
            cancellationToken);

        await create.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Subscriber_Should_FailToStart_When_NoHandlerIsRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SubscriberTestHost host = await CreateHostAsync(_ => { }, cancellationToken);

        Func<Task> start = () => host.StartAsync(cancellationToken);

        await start.Should().ThrowAsync<InvalidOperationException>();
    }

    private Task<SubscriberTestHost> CreateHostAsync(Action<EventBusSubscriberBuilder> registerHandlers, CancellationToken cancellationToken)
        => SubscriberTestHost.CreateAsync(server, database, SubscribingTestNames.NewSubscriber(), registerHandlers, TimeProvider.System, cancellationToken);
}
