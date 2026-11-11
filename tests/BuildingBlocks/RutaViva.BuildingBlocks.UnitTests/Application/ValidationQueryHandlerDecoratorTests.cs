using FluentValidation;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class ValidationQueryHandlerDecoratorTests
{
    private const string ValidName = "name";
    private const string ExpectedValue = "expected";

    private readonly IQueryHandler<SampleQuery, string> _inner = Substitute.For<IQueryHandler<SampleQuery, string>>();

    private ValidationQueryHandlerDecorator<SampleQuery, string> CreateDecorator(params IValidator<SampleQuery>[] validators) =>
        new(_inner, validators);

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationError_When_QueryIsInvalid()
    {
        ValidationQueryHandlerDecorator<SampleQuery, string> decorator = CreateDecorator(new SampleQueryValidator());

        Result<string> result = await decorator.HandleAsync(new SampleQuery(string.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain(SampleQueryValidator.NameRequiredMessage);
    }

    [Fact]
    public async Task HandleAsync_Should_NotInvokeInnerHandler_When_QueryIsInvalid()
    {
        ValidationQueryHandlerDecorator<SampleQuery, string> decorator = CreateDecorator(new SampleQueryValidator());

        await decorator.HandleAsync(new SampleQuery(string.Empty), TestContext.Current.CancellationToken);

        await _inner.DidNotReceive().HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInnerResult_When_QueryIsValid()
    {
        _inner.HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Success(ExpectedValue));
        ValidationQueryHandlerDecorator<SampleQuery, string> decorator = CreateDecorator(new SampleQueryValidator());

        Result<string> result = await decorator.HandleAsync(new SampleQuery(ValidName), TestContext.Current.CancellationToken);

        result.Value.Should().Be(ExpectedValue);
    }

    [Fact]
    public async Task HandleAsync_Should_InvokeInnerHandler_When_NoValidatorsAreRegistered()
    {
        _inner.HandleAsync(Arg.Any<SampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Success(ExpectedValue));
        ValidationQueryHandlerDecorator<SampleQuery, string> decorator = CreateDecorator();

        Result<string> result = await decorator.HandleAsync(new SampleQuery(string.Empty), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
