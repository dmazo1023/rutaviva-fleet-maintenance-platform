using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.IntegrationTests.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Messaging;

[Trait("Category", "Integration")]
public sealed class EfEventCheckpointStoreTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const long NoOffset = 0;
    private const long FirstOffset = 42;
    private const long SecondOffset = 77;

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetLastOffsetAsync_Should_ReturnZero_When_SubscriberHasNoCheckpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        EfEventCheckpointStore<TestDbContext> store = new(context, new FakeClock(Now));

        long offset = await store.GetLastOffsetAsync(Guid.NewGuid().ToString("N"), cancellationToken);

        offset.Should().Be(NoOffset);
    }

    [Fact]
    public async Task SaveAsync_Should_PersistOffset_When_ChangesAreSaved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string subscriber = Guid.NewGuid().ToString("N");
        await using TestDbContext writer = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        EfEventCheckpointStore<TestDbContext> writerStore = new(writer, new FakeClock(Now));

        await writerStore.SaveAsync(subscriber, FirstOffset, cancellationToken);
        await writer.SaveChangesAsync(cancellationToken);

        long offset = await ReadOffsetAsync(subscriber, cancellationToken);
        offset.Should().Be(FirstOffset);
    }

    [Fact]
    public async Task SaveAsync_Should_UpdateOffset_When_CheckpointAlreadyExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string subscriber = Guid.NewGuid().ToString("N");
        await using TestDbContext writer = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        EfEventCheckpointStore<TestDbContext> writerStore = new(writer, new FakeClock(Now));
        await writerStore.SaveAsync(subscriber, FirstOffset, cancellationToken);
        await writer.SaveChangesAsync(cancellationToken);

        await writerStore.SaveAsync(subscriber, SecondOffset, cancellationToken);
        await writer.SaveChangesAsync(cancellationToken);

        long offset = await ReadOffsetAsync(subscriber, cancellationToken);
        offset.Should().Be(SecondOffset);
    }

    private async Task<long> ReadOffsetAsync(string subscriber, CancellationToken cancellationToken)
    {
        await using TestDbContext reader = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        EfEventCheckpointStore<TestDbContext> readerStore = new(reader, new FakeClock(Now));
        return await readerStore.GetLastOffsetAsync(subscriber, cancellationToken);
    }
}
