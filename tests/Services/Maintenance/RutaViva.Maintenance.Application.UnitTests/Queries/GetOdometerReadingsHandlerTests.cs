using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Application.Telemetry.Queries;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

public sealed class GetOdometerReadingsHandlerTests
{
    private const int ValidLimit = 50;
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 200;
    private const int LimitBelowMinimum = 0;
    private const int LimitAboveMaximum = 201;

    private readonly IOdometerReadingQueries _readings = Substitute.For<IOdometerReadingQueries>();
    private readonly IQueryHandler<GetOdometerReadingsQuery, IReadOnlyList<OdometerReadingDto>> _inner =
        Substitute.For<IQueryHandler<GetOdometerReadingsQuery, IReadOnlyList<OdometerReadingDto>>>();

    private readonly GetOdometerReadingsQueryValidator _validator = new();

    [Fact]
    public async Task HandleAsync_Should_DelegateToPort_When_QueryIsValid()
    {
        GetOdometerReadingsQuery query = new(VehicleId.New(), ValidLimit);
        IReadOnlyList<OdometerReadingDto> readings = [QueryTestData.Reading()];
        _readings.GetRecentAsync(query.VehicleId, ValidLimit, Arg.Any<CancellationToken>()).Returns(Task.FromResult(readings));
        GetOdometerReadingsHandler handler = new(_readings);

        Result<IReadOnlyList<OdometerReadingDto>> result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(readings);
        await _readings.Received(1).GetRecentAsync(query.VehicleId, ValidLimit, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(LimitBelowMinimum)]
    [InlineData(LimitAboveMaximum)]
    public async Task HandleAsync_Should_ReturnValidationWithoutDelegating_When_LimitIsOutOfRange(int limit)
    {
        GetOdometerReadingsQuery query = new(VehicleId.New(), limit);
        ValidationQueryHandlerDecorator<GetOdometerReadingsQuery, IReadOnlyList<OdometerReadingDto>> decorator = new(_inner, [_validator]);

        Result<IReadOnlyList<OdometerReadingDto>> result = await decorator.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _inner.DidNotReceive().HandleAsync(Arg.Any<GetOdometerReadingsQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MinimumLimit)]
    [InlineData(MaximumLimit)]
    public void Validate_Should_Pass_When_LimitIsOnTheBoundary(int limit)
    {
        GetOdometerReadingsQuery query = new(VehicleId.New(), limit);

        bool isValid = _validator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }
}
