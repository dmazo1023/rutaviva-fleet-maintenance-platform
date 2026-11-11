using Microsoft.EntityFrameworkCore;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;
using RutaViva.Testing.Fixtures;

namespace RutaViva.EventBus.Hub.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class SingleWriterEventStoreTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string StreamPrefix = "tests.";
    private const string GuidFormat = "N";
    private const string SubscriberPrefix = "maintenance-";
    private const string FailureText = "Timeout";
    private const long FailedOffset = 57;
    private const long FromStart = 0;
    private const int ParallelPublications = 50;
    private const int PageSize = 100;
    private const int SmallPage = 2;
    private const int ExpectedSingleRow = 1;

    [Fact]
    public async Task AppendAsync_Should_ReturnSameOffsetAndKeepOneRow_When_MessageIdIsPublishedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        IntegrationEventEnvelope envelope = TestEnvelopes.Create();

        long first = await harness.Store.AppendAsync(NewStream(), envelope, cancellationToken);
        long second = await harness.Store.AppendAsync(NewStream(), envelope, cancellationToken);

        second.Should().Be(first);
        int rows = await CountByMessageIdAsync(harness, envelope.Id, cancellationToken);
        rows.Should().Be(ExpectedSingleRow);
    }

    [Fact]
    public async Task AppendAsync_Should_ReturnSameOffset_When_TwoWritersPublishTheSameMessageConcurrently()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness firstWriter = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        await using EventBusStoreHarness secondWriter = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        IntegrationEventEnvelope envelope = TestEnvelopes.Create();
        string stream = NewStream();

        long[] offsets = await Task.WhenAll(
            firstWriter.Store.AppendAsync(stream, envelope, cancellationToken),
            secondWriter.Store.AppendAsync(stream, envelope, cancellationToken));

        offsets[1].Should().Be(offsets[0]);
        int rows = await CountByMessageIdAsync(firstWriter, envelope.Id, cancellationToken);
        rows.Should().Be(ExpectedSingleRow);
    }

    [Fact]
    public async Task AppendAsync_Should_AssignDistinctConsecutiveOffsets_When_PublishedInParallel()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        string stream = NewStream();

        long[] offsets = await Task.WhenAll(
            Enumerable.Range(0, ParallelPublications)
                .Select(_ => harness.Store.AppendAsync(stream, TestEnvelopes.Create(), cancellationToken)));

        long first = offsets.Min();
        IEnumerable<long> expected = Enumerable.Range(0, ParallelPublications).Select(index => first + index);
        offsets.Order().Should().Equal(expected);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnOnlyRequestedStreamsInOrder_When_StreamsAreInterleaved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        string streamA = NewStream();
        string streamB = NewStream();
        string streamC = NewStream();
        long a1 = await AppendNewAsync(harness, streamA, cancellationToken);
        await AppendNewAsync(harness, streamC, cancellationToken);
        long b1 = await AppendNewAsync(harness, streamB, cancellationToken);
        long a2 = await AppendNewAsync(harness, streamA, cancellationToken);

        IReadOnlyList<EventRecord> records = await harness.Store.ReadAsync([streamA, streamB], FromStart, PageSize, cancellationToken);

        records.Select(record => record.Offset).Should().Equal(a1, b1, a2);
    }

    [Fact]
    public async Task ReadAsync_Should_SkipEventsUpToAfterOffset_When_AfterOffsetIsGiven()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        string stream = NewStream();
        long first = await AppendNewAsync(harness, stream, cancellationToken);
        long second = await AppendNewAsync(harness, stream, cancellationToken);

        IReadOnlyList<EventRecord> records = await harness.Store.ReadAsync([stream], first, PageSize, cancellationToken);

        records.Select(record => record.Offset).Should().Equal(second);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnAtMostMax_When_MoreEventsExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        string stream = NewStream();
        for (int index = 0; index <= SmallPage; index++)
        {
            await AppendNewAsync(harness, stream, cancellationToken);
        }

        IReadOnlyList<EventRecord> records = await harness.Store.ReadAsync([stream], FromStart, SmallPage, cancellationToken);

        records.Should().HaveCount(SmallPage);
    }

    [Fact]
    public async Task AddDeadLetterAsync_Should_PersistRow_When_FailureIsReported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using EventBusStoreHarness harness = await EventBusStoreHarness.CreateAsync(database, cancellationToken);
        string subscriber = SubscriberPrefix + Guid.NewGuid().ToString(GuidFormat);

        await harness.Store.AddDeadLetterAsync(subscriber, FailedOffset, FailureText, cancellationToken);

        await using EventBusDbContext context = await harness.ContextFactory.CreateDbContextAsync(cancellationToken);
        DeadLetter row = await context.DeadLetters.AsNoTracking().SingleAsync(deadLetter => deadLetter.SubscriberName == subscriber, cancellationToken);
        row.Offset.Should().Be(FailedOffset);
        row.Error.Should().Be(FailureText);
    }

    private static string NewStream() => StreamPrefix + Guid.NewGuid().ToString(GuidFormat);

    private static Task<long> AppendNewAsync(EventBusStoreHarness harness, string stream, CancellationToken cancellationToken) =>
        harness.Store.AppendAsync(stream, TestEnvelopes.Create(), cancellationToken);

    private static async Task<int> CountByMessageIdAsync(EventBusStoreHarness harness, Guid messageId, CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await harness.ContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Events.CountAsync(stored => stored.MessageId == messageId, cancellationToken);
    }
}
