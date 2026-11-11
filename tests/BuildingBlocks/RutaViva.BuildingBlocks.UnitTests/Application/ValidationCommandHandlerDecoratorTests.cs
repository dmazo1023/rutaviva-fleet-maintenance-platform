using FluentValidation;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class ValidationCommandHandlerDecoratorTests
{
    private readonly ICommandHandler<SampleCommand, Guid> _inner = Substitute.For<ICommandHandler<SampleCommand, Guid>>();

    private ValidationCommandHandlerDecorator<SampleCommand, Guid> CreateDecorator(params IValidator<SampleCommand>[] validators) =>
        new(_inner, validators);

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationError_When_CommandIsInvalid()
    {
        ValidationCommandHandlerDecorator<SampleCommand, Guid> decorator = CreateDecorator(new SampleCommandValidator());

        Result<Guid> result = await decorator.HandleAsync(new SampleCommand(string.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain(SampleCommandValidator.NameRequiredMessage);
    }

    [Fact]
    public async Task HandleAsync_Should_NotInvokeInnerHandler_When_CommandIsInvalid()
    {
        ValidationCommandHandlerDecorator<SampleCommand, Guid> decorator = CreateDecorator(new SampleCommandValidator());

        await decorator.HandleAsync(new SampleCommand(string.Empty), TestContext.Current.CancellationToken);

        await _inner.DidNotReceive().HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInnerResult_When_CommandIsValid()
    {
        Guid expectedId = Guid.NewGuid();
        _inner.HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Guid>.Success(expectedId));
        ValidationCommandHandlerDecorator<SampleCommand, Guid> decorator = CreateDecorator(new SampleCommandValidator());

        Result<Guid> result = await decorator.HandleAsync(new SampleCommand("name"), TestContext.Current.CancellationToken);

        result.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task HandleAsync_Should_InvokeInnerHandler_When_NoValidatorsAreRegistered()
    {
        _inner.HandleAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result<Guid>.Success(Guid.NewGuid()));
        ValidationCommandHandlerDecorator<SampleCommand, Guid> decorator = CreateDecorator();

        Result<Guid> result = await decorator.HandleAsync(new SampleCommand(string.Empty), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
