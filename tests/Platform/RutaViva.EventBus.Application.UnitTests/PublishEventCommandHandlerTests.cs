using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Publishing;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class PublishEventCommandHandlerTests
{
    private const string Stream = "fleet.vehicle";
    private const long StoredOffset = 42;

    private readonly IEventStore _store = Substitute.For<IEventStore>();
    private readonly PublishEventCommandHandler _handler;

    public PublishEventCommandHandlerTests()
    {
        _handler = new PublishEventCommandHandler(_store);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fleet.VehicleRegistered.v1")]
    [InlineData("rutaviva.fleet.VehicleRegistered")]
    [InlineData("rutaviva.fleet.vehicleRegistered.v1")]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_TypeIsMissingOrMalformed(string type)
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithType(type).Build();

        Result<long> result = await _handler.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _store.DidNotReceiveWithAnyArgs().AppendAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_IdIsEmpty()
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithId(Guid.Empty).Build();

        Result<long> result = await _handler.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        result.Error.Type.Should().Be(ErrorType.Validation);
        await _store.DidNotReceiveWithAnyArgs().AppendAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnStoreOffset_When_EnvelopeIsValid()
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().Build();
        _store.AppendAsync(Stream, envelope, TestContext.Current.CancellationToken).Returns(StoredOffset);

        Result<long> result = await _handler.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(StoredOffset);
    }
}
