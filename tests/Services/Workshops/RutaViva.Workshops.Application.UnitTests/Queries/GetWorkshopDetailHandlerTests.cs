using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Application.Workshops.Queries;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.UnitTests.Queries;

public sealed class GetWorkshopDetailHandlerTests
{
    private readonly IWorkshopQueries _workshops = Substitute.For<IWorkshopQueries>();
    private readonly GetWorkshopDetailHandler _handler;

    public GetWorkshopDetailHandlerTests()
    {
        _handler = new GetWorkshopDetailHandler(_workshops);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_WorkshopDoesNotExist()
    {
        GetWorkshopDetailQuery query = new(WorkshopId.New());
        _workshops.GetDetailAsync(query.WorkshopId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkshopDetailDto?>(null));

        Result<WorkshopDetailDto> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkshopErrors.NotFound);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnDetailFromPort_When_WorkshopExists()
    {
        GetWorkshopDetailQuery query = new(WorkshopId.New());
        WorkshopDetailDto detail = new(
            query.WorkshopId.Value,
            WorkshopTestFactory.WorkshopName,
            WorkshopTestFactory.TaxIdValue,
            WorkshopTestFactory.EmailValue,
            WorkshopTestFactory.PhoneValue,
            WorkshopTestFactory.AddressValue,
            WorkshopTestFactory.CityValue,
            WorkshopStatus.Active,
            DateTimeOffset.UnixEpoch);
        _workshops.GetDetailAsync(query.WorkshopId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkshopDetailDto?>(detail));

        Result<WorkshopDetailDto> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(detail);
    }
}
