using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class LoggingQueryHandlerDecoratorTests
{
    private const string ValidName = "name";
    private const string StartedText = "started";
    private const string SucceededText = "succeeded";
    private const string FailedText = "failed";
    private const string ErrorCode = "Sample.Failed";
    private const string ErrorDescription = "Sample failure.";
    private const int StartAndEndMessageCount = 2;
    private const int StartIndex = 0;
    private const int EndIndex = 1;

    private readonly IQueryHandler<SampleQuery, string> _inner = Substitute.For<IQueryHandler<SampleQuery, string>>();
    private readonly CapturingLogger<LoggingQueryHandlerDecorator<SampleQuery, string>> _logger = new();

    private LoggingQueryHandlerDecorator<SampleQuery, string> CreateDecorator() => new(_inner, _logger);

    [Fact]
    public async Task HandleAsync_Should_LogStartAndSuccess_When_HandlerSucceeds()
    {
        _inner.HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Success(ValidName));

        await CreateDecorator().HandleAsync(new SampleQuery(ValidName), TestContext.Current.CancellationToken);

        _logger.Messages.Should().HaveCount(StartAndEndMessageCount);
        _logger.Messages[StartIndex].Should().Contain(nameof(SampleQuery)).And.Contain(StartedText);
        _logger.Messages[EndIndex].Should().Contain(nameof(SampleQuery)).And.Contain(SucceededText);
    }

    [Fact]
    public async Task HandleAsync_Should_LogStartAndErrorCode_When_HandlerFails()
    {
        Error error = new(ErrorCode, ErrorDescription, ErrorType.NotFound);
        _inner.HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Failure(error));

        await CreateDecorator().HandleAsync(new SampleQuery(ValidName), TestContext.Current.CancellationToken);

        _logger.Messages.Should().HaveCount(StartAndEndMessageCount);
        _logger.Messages[StartIndex].Should().Contain(StartedText);
        _logger.Messages[EndIndex].Should().Contain(FailedText).And.Contain(error.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInnerResult_When_LoggingOutcome()
    {
        Error error = new(ErrorCode, ErrorDescription, ErrorType.Conflict);
        _inner.HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Failure(error));

        Result<string> result = await CreateDecorator().HandleAsync(new SampleQuery(ValidName), TestContext.Current.CancellationToken);

        result.Error.Should().Be(error);
    }
}
