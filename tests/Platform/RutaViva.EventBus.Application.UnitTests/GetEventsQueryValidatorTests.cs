using FluentValidation.Results;
using RutaViva.EventBus.Application.Queries;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.UnitTests;

public sealed class GetEventsQueryValidatorTests
{
    private const string Stream = "fleet.vehicle";
    private const char Filler = 'a';
    private const long AfterOffset = 10;
    private const int Max = 50;
    private const string BlankStream = " ";

    private readonly GetEventsQueryValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_Should_Pass_When_QueryIsValid()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([Stream], AfterOffset, Max), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_StreamsAreEmpty()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([], AfterOffset, Max), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.StreamsMissing.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_StreamsAreNull()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery(null!, AfterOffset, Max), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.StreamsMissing.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_StreamExceedsMaxLength()
    {
        string stream = new(Filler, IntegrationEventLimits.StreamMaxLength + 1);

        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([stream], AfterOffset, Max), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.StreamTooLong.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_StreamIsBlank()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([BlankStream], AfterOffset, Max), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.StreamMissing.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_OffsetIsNegative()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([Stream], -1, Max), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.OffsetInvalid.Code);
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_MaxIsNotPositive()
    {
        ValidationResult result = await _validator.ValidateAsync(new GetEventsQuery([Stream], AfterOffset, 0), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(EventBusErrors.MaxInvalid.Code);
    }
}
