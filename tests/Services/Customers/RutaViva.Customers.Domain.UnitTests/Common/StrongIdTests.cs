using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.UnitTests.Common;

public sealed class StrongIdTests
{
    private const string EmptyCode = "Identifier.Empty";

    [Fact]
    public void New_Should_ReturnDistinctNonEmptyIds_When_CalledTwice()
    {
        CustomerId first = CustomerId.New();
        CustomerId second = CustomerId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
        ApproverId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_ReturnEmptyIdentifierError_When_GuidIsEmpty()
    {
        Result<CustomerId> customer = CustomerId.Create(Guid.Empty);
        Result<ApproverId> approver = ApproverId.Create(Guid.Empty);

        customer.Error.Code.Should().Be(EmptyCode);
        customer.Error.Type.Should().Be(ErrorType.Validation);
        approver.Error.Code.Should().Be(EmptyCode);
    }

    [Fact]
    public void Create_Should_ReturnIdWithSameValue_When_GuidIsNotEmpty()
    {
        Guid value = Guid.NewGuid();

        Result<CustomerId> customer = CustomerId.Create(value);
        Result<ApproverId> approver = ApproverId.Create(value);

        customer.Value.Value.Should().Be(value);
        approver.Value.Value.Should().Be(value);
        customer.Value.ToString().Should().Be(value.ToString());
    }

    [Fact]
    public void Constructor_Should_Throw_When_GuidIsEmpty()
    {
        Action act = () => _ = new CustomerId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
