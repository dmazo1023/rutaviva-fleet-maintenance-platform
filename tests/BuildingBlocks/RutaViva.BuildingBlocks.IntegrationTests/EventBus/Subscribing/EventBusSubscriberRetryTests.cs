using RutaViva.BuildingBlocks.IntegrationTests.Messaging;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

[Trait("Category", "Integration")]
public sealed class EventBusSubscriberRetryTests(EventBusTestServer server, SqlServerTestDatabase database)
    : IClassFixture<EventBusTestServer>, IClassFixture<SqlServerTestDatabase>
{
    private const int ExpectedAttempts = 4;
    private const int ExpectedSingleAttempt = 1;
    private const int ExpectedDeadLetters = 1;
    private const int ProcessTimeoutSeconds = 30;

    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(ProcessTimeoutSeconds);

    [Fact]
    public async Task Subscriber_Should_RetryAndReportOneDeadLetterAndContinue_When_HandlerAlwaysThrows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await SubscriberTestHost.CreateAsync(
            server,
            database,
            SubscribingTestNames.NewSubscriber(),
            builder => builder
                .Handle<FailingHappenedV1, FailingEffectHandler>(stream)
                .Handle<SampleHappenedV1, RecordEffectHandler>(stream),
            new ImmediateTimeProvider(),
            cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        await publisher.PublishAsync(stream, FailingHappenedV1.EventName, new FailingHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);
        long next = await publisher.PublishAsync(stream, SampleHappenedV1.EventName, new SampleHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        await host.StartAsync(cancellationToken);

        bool processed = await host.WaitForCheckpointAsync(next, ProcessTimeout, cancellationToken);
        List<long> offsets = await host.ReadEffectOffsetsAsync(subject, cancellationToken);
        int deadLetters = await DeadLetterQuery.CountAsync(server.ConnectionString, host.SubscriberName, cancellationToken);
        processed.Should().BeTrue();
        host.Attempts.Should().Be(ExpectedAttempts);
        deadLetters.Should().Be(ExpectedDeadLetters);
        offsets.Should().Equal(next);
    }

    [Fact]
    public async Task Subscriber_Should_NotRetryAndReportDeadLetter_When_HandlerReturnsValidationFailure()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string stream = SubscribingTestNames.NewStream();
        string subject = SubscribingTestNames.NewSubject();
        await using SubscriberTestHost host = await SubscriberTestHost.CreateAsync(
            server,
            database,
            SubscribingTestNames.NewSubscriber(),
            builder => builder.Handle<FailingHappenedV1, RejectingEffectHandler>(stream),
            new ImmediateTimeProvider(),
            cancellationToken);
        await using TestEventPublisher publisher = await TestEventPublisher.ConnectAsync(server, cancellationToken);
        long offset = await publisher.PublishAsync(stream, FailingHappenedV1.EventName, new FailingHappenedV1(Guid.NewGuid()), subject, Guid.NewGuid(), cancellationToken);

        await host.StartAsync(cancellationToken);

        bool processed = await host.WaitForCheckpointAsync(offset, ProcessTimeout, cancellationToken);
        int deadLetters = await DeadLetterQuery.CountAsync(server.ConnectionString, host.SubscriberName, cancellationToken);
        processed.Should().BeTrue();
        host.Attempts.Should().Be(ExpectedSingleAttempt);
        deadLetters.Should().Be(ExpectedDeadLetters);
    }
}
