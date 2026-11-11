using FluentValidation;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Publishing;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class PublishEventCommandValidatorTests
{
    private const string Stream = "fleet.vehicle";
    private const char Filler = 'a';

    private readonly IEventStore _store = Substitute.For<IEventStore>();
    private readonly ValidationCommandHandlerDecorator<PublishEventCommand, long> _pipeline;

    public PublishEventCommandValidatorTests()
    {
        IValidator<PublishEventCommand>[] validators = [new PublishEventCommandValidator()];
        _pipeline = new ValidationCommandHandlerDecorator<PublishEventCommand, long>(new PublishEventCommandHandler(_store), validators);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fleet.VehicleRegistered.v1")]
    [InlineData("rutaviva.fleet.VehicleRegistered")]
    [InlineData("rutaviva.fleet.VehicleRegistered.1")]
    [InlineData("rutaviva.fleet.VehicleRegistered.v0")]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_TypeIsMissingOrNotParseable(string type)
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithType(type).Build();

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_StreamExceedsMaxLength()
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().Build();
        string stream = new(Filler, IntegrationEventLimits.StreamMaxLength + 1);

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(stream, envelope), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_SubjectExceedsMaxLength()
    {
        string subject = new(Filler, IntegrationEventLimits.SubjectMaxLength + 1);
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithSubject(subject).Build();

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_SourceExceedsMaxLength()
    {
        string source = new(Filler, IntegrationEventLimits.SourceMaxLength + 1);
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithSource(source).Build();

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_IdIsEmpty()
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().WithId(Guid.Empty).Build();

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(Stream, envelope), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationAndSkipStore_When_EnvelopeIsNull()
    {
        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(Stream, null!), TestContext.Current.CancellationToken);

        AssertValidationAndStoreUntouched(result);
    }

    [Fact]
    public async Task HandleAsync_Should_AppendToStore_When_StreamAtMaxLengthAndEnvelopeIsValid()
    {
        IntegrationEventEnvelope envelope = new IntegrationEventEnvelopeBuilder().Build();
        string stream = new(Filler, IntegrationEventLimits.StreamMaxLength);

        Result<long> result = await _pipeline.HandleAsync(new PublishEventCommand(stream, envelope), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _store.Received(1).AppendAsync(stream, envelope, Arg.Any<CancellationToken>());
    }

    private void AssertValidationAndStoreUntouched(Result<long> result)
    {
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _store.ReceivedCalls().Should().BeEmpty();
    }
}
