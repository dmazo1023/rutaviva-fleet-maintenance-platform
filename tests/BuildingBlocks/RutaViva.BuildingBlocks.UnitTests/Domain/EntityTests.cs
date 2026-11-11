using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.UnitTests.Domain;

public sealed class EntityTests
{
    private static readonly Guid SharedId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Equals_Should_ReturnTrue_When_SameTypeAndSameId()
    {
        FirstEntity left = new(SharedId);
        FirstEntity right = new(SharedId);

        left.Equals(right).Should().BeTrue();
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_DifferentConcreteTypeWithSameId()
    {
        FirstEntity left = new(SharedId);
        SecondEntity right = new(SharedId);

        left.Equals(right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_IdsDiffer()
    {
        FirstEntity left = new(SharedId);
        FirstEntity right = new(Guid.NewGuid());

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_BothIdsAreDefault()
    {
        FirstEntity left = new(Guid.Empty);
        FirstEntity right = new(Guid.Empty);

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_DefaultIdAndSameReference()
    {
        FirstEntity entity = new(Guid.Empty);
        FirstEntity sameReference = entity;

        (entity == sameReference).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsNull()
    {
        FirstEntity entity = new(SharedId);

        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (entity != null).Should().BeTrue();
    }

    private sealed class FirstEntity : Entity<Guid>
    {
        public FirstEntity(Guid id)
        {
            Id = id;
        }
    }

    private sealed class SecondEntity : Entity<Guid>
    {
        public SecondEntity(Guid id)
        {
            Id = id;
        }
    }
}
