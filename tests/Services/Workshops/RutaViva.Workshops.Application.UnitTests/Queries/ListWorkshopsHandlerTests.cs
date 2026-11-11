using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Application.Workshops.Queries;
using RutaViva.Workshops.Domain;

namespace RutaViva.Workshops.Application.UnitTests.Queries;

public sealed class ListWorkshopsHandlerTests
{
    private readonly IWorkshopQueries _workshops = Substitute.For<IWorkshopQueries>();
    private readonly ListWorkshopsHandler _handler;

    public ListWorkshopsHandlerTests()
    {
        _handler = new ListWorkshopsHandler(_workshops);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSummariesFromPort_When_FiltersAreGiven()
    {
        ListWorkshopsQuery query = new(WorkshopStatus.Active, WorkshopTestFactory.CityValue);
        IReadOnlyList<WorkshopSummaryDto> summaries =
        [
            new WorkshopSummaryDto(
                Guid.NewGuid(),
                WorkshopTestFactory.WorkshopName,
                WorkshopTestFactory.TaxIdValue,
                WorkshopTestFactory.CityValue,
                WorkshopStatus.Active)
        ];
        _workshops.ListAsync(query.Status, query.City, Arg.Any<CancellationToken>()).Returns(Task.FromResult(summaries));

        Result<IReadOnlyList<WorkshopSummaryDto>> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(summaries);
        await _workshops.Received(1).ListAsync(query.Status, query.City, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnEmptyList_When_NoWorkshopMatches()
    {
        ListWorkshopsQuery query = new(null, null);
        IReadOnlyList<WorkshopSummaryDto> empty = [];
        _workshops.ListAsync(null, null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(empty));

        Result<IReadOnlyList<WorkshopSummaryDto>> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
