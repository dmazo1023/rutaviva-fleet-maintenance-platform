using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.UnitTests.Common;

public sealed class StrongIdTests
{
    private const string EmptyCode = "Identifier.Empty";

    [Fact]
    public void New_Should_ReturnDistinctNonEmptyIds_When_CalledTwice()
    {
        VehicleId first = VehicleId.New();
        VehicleId second = VehicleId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }

    [Fact]
    public void Create_Should_ReturnEmptyIdentifierError_When_GuidIsEmpty()
    {
        Result<VehicleId> vehicle = VehicleId.Create(Guid.Empty);
        Result<CustomerId> customer = CustomerId.Create(Guid.Empty);
        Result<WorkshopId> workshop = WorkshopId.Create(Guid.Empty);
        Result<ApproverId> approver = ApproverId.Create(Guid.Empty);

        vehicle.Error.Code.Should().Be(EmptyCode);
        customer.Error.Code.Should().Be(EmptyCode);
        workshop.Error.Code.Should().Be(EmptyCode);
        approver.Error.Code.Should().Be(EmptyCode);
    }

    [Fact]
    public void Create_Should_ReturnIdWithSameValue_When_GuidIsNotEmpty()
    {
        Guid value = Guid.NewGuid();

        Result<ApproverId> result = ApproverId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
        result.Value.Should().Be(new ApproverId(value));
    }

    [Fact]
    public void Constructor_Should_Throw_When_GuidIsEmpty()
    {
        Action act = () => _ = new WorkshopId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_Should_ReturnGuidText_When_IdIsValid()
    {
        Guid value = Guid.NewGuid();

        new CustomerId(value).ToString().Should().Be(value.ToString());
    }
}
