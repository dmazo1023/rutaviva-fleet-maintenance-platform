using NSubstitute;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Application.RoutineTypes.Queries;

namespace RutaViva.Maintenance.Application.UnitTests.Queries;

public sealed class GetRoutineTypesHandlerTests
{
    private readonly IRoutineTypeQueries _routineTypes = Substitute.For<IRoutineTypeQueries>();
    private readonly GetRoutineTypesHandler _handler;

    public GetRoutineTypesHandlerTests()
    {
        _handler = new GetRoutineTypesHandler(_routineTypes);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTypesFromPort_When_QueryIsHandled()
    {
        IReadOnlyList<RoutineTypeDto> types = [QueryTestData.RoutineType()];
        _routineTypes.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(types));

        Result<IReadOnlyList<RoutineTypeDto>> result = await _handler.HandleAsync(new GetRoutineTypesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(types);
        await _routineTypes.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
