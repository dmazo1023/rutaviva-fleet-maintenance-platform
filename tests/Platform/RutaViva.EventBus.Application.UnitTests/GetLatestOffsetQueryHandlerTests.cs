using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Queries;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class GetLatestOffsetQueryHandlerTests
{
    private const long Latest = 99;

    [Fact]
    public async Task HandleAsync_Should_ReturnStoreLatestOffset_When_Queried()
    {
        IEventStore store = Substitute.For<IEventStore>();
        store.GetLatestOffsetAsync(TestContext.Current.CancellationToken).Returns(Latest);
        GetLatestOffsetQueryHandler handler = new(store);

        Result<long> result = await handler.HandleAsync(new GetLatestOffsetQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(Latest);
    }
}
