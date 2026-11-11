using FluentValidation.Results;
using RutaViva.EventBus.Application.Failures;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class ReportFailureCommandValidatorTests
{
    private const string Subscriber = "maintenance";
    private const string FailureText = "Timeout";
    private const char Filler = 'a';
    private const long Offset = 57;
    private const string BlankText = " ";

    private readonly ReportFailureCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_Should_Pass_When_CommandIsValid()
    {
        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(Subscriber, Offset, FailureText), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_SubscriberIsBlank()
    {
        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(BlankText, Offset, FailureText), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.SubscriberMissing.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_SubscriberExceedsMaxLength()
    {
        string subscriber = new(Filler, IntegrationEventLimits.SubscriberNameMaxLength + 1);

        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(subscriber, Offset, FailureText), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.SubscriberTooLong.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_OffsetIsNegative()
    {
        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(Subscriber, -1, FailureText), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.OffsetInvalid.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_ErrorIsBlank()
    {
        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(Subscriber, Offset, BlankText), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.FailureErrorMissing.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_ErrorExceedsMaxLength()
    {
        string error = new(Filler, IntegrationEventLimits.FailureMaxLength + 1);

        ValidationResult result = await _validator.ValidateAsync(new ReportFailureCommand(Subscriber, Offset, error), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.FailureErrorTooLong.Code);
    }
}
