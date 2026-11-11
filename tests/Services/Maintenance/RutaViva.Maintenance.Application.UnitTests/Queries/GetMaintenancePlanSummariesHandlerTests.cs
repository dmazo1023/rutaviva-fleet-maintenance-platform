using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Plans.Queries;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

public sealed class GetMaintenancePlanSummariesHandlerTests
{
    private readonly IMaintenancePlanQueries _plans = Substitute.For<IMaintenancePlanQueries>();
    private readonly GetMaintenancePlanSummariesHandler _handler;

    public GetMaintenancePlanSummariesHandlerTests()
    {
        _handler = new GetMaintenancePlanSummariesHandler(_plans);
    }

    [Fact]
    public async Task HandleAsync_Should_DelegateToPortWithCustomer_When_CustomerIsGiven()
    {
        CustomerId customerId = CustomerId.New();
        IReadOnlyList<MaintenancePlanSummaryDto> summaries = [QueryTestData.PlanSummary()];
        _plans.GetSummariesAsync(customerId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(summaries));

        Result<IReadOnlyList<MaintenancePlanSummaryDto>> result = await _handler.HandleAsync(
            new GetMaintenancePlanSummariesQuery(customerId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(summaries);
        await _plans.Received(1).GetSummariesAsync(customerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_DelegateToPortWithoutCustomer_When_CustomerIsMissing()
    {
        IReadOnlyList<MaintenancePlanSummaryDto> summaries = [];
        _plans.GetSummariesAsync(null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(summaries));

        Result<IReadOnlyList<MaintenancePlanSummaryDto>> result = await _handler.HandleAsync(
            new GetMaintenancePlanSummariesQuery(null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _plans.Received(1).GetSummariesAsync(null, Arg.Any<CancellationToken>());
    }
}
