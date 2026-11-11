using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Application.Failures;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class ReportFailureCommandHandlerTests
{
    private const string Subscriber = "maintenance";
    private const long Offset = 57;
    private const string FailureText = "Timeout";

    private readonly IEventStore _store = Substitute.For<IEventStore>();
    private readonly ReportFailureCommandHandler _handler;

    public ReportFailureCommandHandlerTests()
    {
        _handler = new ReportFailureCommandHandler(_store);
    }

    [Fact]
    public async Task HandleAsync_Should_DelegateToAddDeadLetter_When_CommandIsValid()
    {
        Result<bool> result = await _handler.HandleAsync(new ReportFailureCommand(Subscriber, Offset, FailureText), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _store.Received(1).AddDeadLetterAsync(Subscriber, Offset, FailureText, Arg.Any<CancellationToken>());
    }
}
