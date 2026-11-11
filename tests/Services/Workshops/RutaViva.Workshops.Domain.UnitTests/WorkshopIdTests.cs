using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.UnitTests;

public sealed class WorkshopIdTests
{
    private const string EmptyCode = "WorkshopId.Empty";

    [Fact]
    public void New_Should_ReturnNonEmptyUniqueIds_When_CalledTwice()
    {
        WorkshopId first = WorkshopId.New();
        WorkshopId second = WorkshopId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }

    [Fact]
    public void Create_Should_WrapTheGuid_When_ValueIsNotEmpty()
    {
        Guid value = Guid.CreateVersion7();

        Result<WorkshopId> result = WorkshopId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_Should_ReturnEmptyError_When_ValueIsEmpty()
    {
        Result<WorkshopId> result = WorkshopId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(EmptyCode);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
