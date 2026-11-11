using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Plans.Queries;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

public sealed class GetMaintenancePlanHandlerTests
{
    private readonly IMaintenancePlanQueries _plans = Substitute.For<IMaintenancePlanQueries>();
    private readonly GetMaintenancePlanHandler _handler;

    public GetMaintenancePlanHandlerTests()
    {
        _handler = new GetMaintenancePlanHandler(_plans);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        GetMaintenancePlanQuery query = new(VehicleId.New());
        _plans.GetDetailAsync(query.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlanDetailDto?>(null));

        Result<MaintenancePlanDetailDto> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnDetailFromPort_When_PlanExists()
    {
        GetMaintenancePlanQuery query = new(VehicleId.New());
        MaintenancePlanDetailDto detail = QueryTestData.PlanDetail(query.VehicleId);
        _plans.GetDetailAsync(query.VehicleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlanDetailDto?>(detail));

        Result<MaintenancePlanDetailDto> result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(detail);
        await _plans.Received(1).GetDetailAsync(query.VehicleId, Arg.Any<CancellationToken>());
    }
}
