using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Application.Alerts.Queries;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

public sealed class SearchAlertsHandlerTests
{
    private const int ValidPage = 2;
    private const int ValidPageSize = 25;
    private const int MinimumPage = 1;
    private const int MinimumPageSize = 1;
    private const int MaximumPageSize = 100;
    private const int PageBelowMinimum = 0;
    private const int PageSizeBelowMinimum = 0;
    private const int PageSizeAboveMaximum = 101;
    private const int TotalCount = 40;

    private readonly IAlertQueries _alerts = Substitute.For<IAlertQueries>();
    private readonly IQueryHandler<SearchAlertsQuery, PagedResult<AlertDto>> _inner =
        Substitute.For<IQueryHandler<SearchAlertsQuery, PagedResult<AlertDto>>>();

    private readonly SearchAlertsQueryValidator _validator = new();

    [Fact]
    public async Task HandleAsync_Should_DelegateCriteriaToPort_When_QueryIsValid()
    {
        VehicleId vehicleId = VehicleId.New();
        CustomerId customerId = CustomerId.New();
        SearchAlertsQuery query = new(AlertStatus.Active, vehicleId, customerId, ValidPage, ValidPageSize);
        PagedResult<AlertDto> page = new([QueryTestData.Alert()], ValidPage, ValidPageSize, TotalCount);
        _alerts.SearchAsync(Arg.Any<AlertSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(page));
        SearchAlertsHandler handler = new(_alerts);

        Result<PagedResult<AlertDto>> result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(page);
        await _alerts.Received(1).SearchAsync(
            Arg.Is<AlertSearchCriteria>(criteria =>
                criteria.Status == AlertStatus.Active
                && criteria.VehicleId == vehicleId
                && criteria.CustomerId == customerId
                && criteria.Page == ValidPage
                && criteria.PageSize == ValidPageSize),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PageBelowMinimum, ValidPageSize)]
    [InlineData(ValidPage, PageSizeBelowMinimum)]
    [InlineData(ValidPage, PageSizeAboveMaximum)]
    public async Task HandleAsync_Should_ReturnValidationWithoutDelegating_When_PagingIsOutOfRange(int page, int pageSize)
    {
        SearchAlertsQuery query = new(null, null, null, page, pageSize);
        ValidationQueryHandlerDecorator<SearchAlertsQuery, PagedResult<AlertDto>> decorator = new(_inner, [_validator]);

        Result<PagedResult<AlertDto>> result = await decorator.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _inner.DidNotReceive().HandleAsync(Arg.Any<SearchAlertsQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MinimumPage, MinimumPageSize)]
    [InlineData(MinimumPage, MaximumPageSize)]
    public void Validate_Should_Pass_When_PagingIsOnTheBoundary(int page, int pageSize)
    {
        SearchAlertsQuery query = new(null, null, null, page, pageSize);

        bool isValid = _validator.Validate(query).IsValid;

        isValid.Should().BeTrue();
    }
}
