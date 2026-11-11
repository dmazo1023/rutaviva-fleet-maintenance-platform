using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Queries;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class GetEventsQueryHandlerTests
{
    private const string Stream = "fleet.vehicle";
    private const int RequestedMax = 500;
    private const int Cap = 100;
    private const long AfterOffset = 10;

    private readonly IEventStore _store = Substitute.For<IEventStore>();
    private readonly GetEventsQueryHandler _handler;

    public GetEventsQueryHandlerTests()
    {
        _handler = new GetEventsQueryHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_Should_CapMaxToHundred_When_MaxExceedsLimit()
    {
        _store.ReadAsync(default!, default, default, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Array.Empty<EventRecord>());

        Result<IReadOnlyList<EventRecord>> result = await _handler.HandleAsync(
            new GetEventsQuery([Stream], AfterOffset, RequestedMax),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _store.Received(1).ReadAsync(Arg.Any<IReadOnlyCollection<string>>(), AfterOffset, Cap, Arg.Any<CancellationToken>());
    }
}
