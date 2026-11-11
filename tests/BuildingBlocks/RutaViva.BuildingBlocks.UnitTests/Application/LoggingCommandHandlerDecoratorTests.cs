using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class LoggingCommandHandlerDecoratorTests
{
    private readonly ICommandHandler<SampleCommand, Guid> _inner = Substitute.For<ICommandHandler<SampleCommand, Guid>>();
    private readonly CapturingLogger<LoggingCommandHandlerDecorator<SampleCommand, Guid>> _logger = new();

    private LoggingCommandHandlerDecorator<SampleCommand, Guid> CreateDecorator() => new(_inner, _logger);

    [Fact]
    public async Task HandleAsync_Should_LogCommandName_When_HandlerSucceeds()
    {
        _inner.HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Guid>.Success(Guid.NewGuid()));

        await CreateDecorator().HandleAsync(new SampleCommand("name"), TestContext.Current.CancellationToken);

        _logger.Messages.Should().ContainSingle().Which.Should().Contain(nameof(SampleCommand));
    }

    [Fact]
    public async Task HandleAsync_Should_LogCommandNameAndErrorCode_When_HandlerFails()
    {
        Error error = new("Sample.Failed", "Sample failure.", ErrorType.BusinessRule);
        _inner.HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(error));

        await CreateDecorator().HandleAsync(new SampleCommand("name"), TestContext.Current.CancellationToken);

        string message = _logger.Messages.Should().ContainSingle().Subject;
        message.Should().Contain(nameof(SampleCommand)).And.Contain(error.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInnerResult_When_LoggingOutcome()
    {
        Error error = new("Sample.Failed", "Sample failure.", ErrorType.Conflict);
        _inner.HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(error));

        Result<Guid> result = await CreateDecorator().HandleAsync(new SampleCommand("name"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(error);
    }
}
