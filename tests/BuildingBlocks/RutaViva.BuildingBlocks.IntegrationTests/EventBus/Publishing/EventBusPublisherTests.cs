using Microsoft.AspNetCore.SignalR.Client;
using NSubstitute;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Publishing;

public sealed class EventBusPublisherTests
{
    private const string UnreachableUrl = "http://127.0.0.1:1/hub";
    private const string Stream = "publisher.stream";
    private const string EventType = "rutaviva.publisher.SampleHappened.v1";
    private const string EventSource = "/rutaviva/publisher";
    private const string EventSubject = "publisher-subject";

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Constructor_Should_NotCreateConnection_When_PublisherIsBuilt()
    {
        IEventBusConnectionFactory factory = Substitute.For<IEventBusConnectionFactory>();

        await using EventBusPublisher publisher = new(factory);

        factory.DidNotReceive().Create();
    }

    [Fact]
    public async Task SendAsync_Should_ThrowUnavailable_When_ServerIsNotReachable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IEventBusConnectionFactory factory = Substitute.For<IEventBusConnectionFactory>();
        factory.Create().Returns(new HubConnectionBuilder().WithUrl(UnreachableUrl).Build());
        await using EventBusPublisher publisher = new(factory);

        Func<Task> act = () => publisher.SendAsync(Stream, CreateEnvelope(), cancellationToken);

        await act.Should().ThrowAsync<EventBusUnavailableException>();
    }

    private static IntegrationEventEnvelope CreateEnvelope() => new()
    {
        Id = Guid.NewGuid(),
        Source = EventSource,
        Type = EventType,
        Subject = EventSubject,
        Time = Now,
        Data = default,
    };
}
