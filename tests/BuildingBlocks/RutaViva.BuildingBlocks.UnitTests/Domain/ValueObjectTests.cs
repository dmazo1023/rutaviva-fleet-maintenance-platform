using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.UnitTests.Domain;

public sealed class ValueObjectTests
{
    private const string Street = "Main";
    private const string OtherStreet = "Second";
    private const int Number = 10;

    [Fact]
    public void Equals_Should_BeTrue_When_ComponentsAreEqual()
    {
        Address first = new(Street, Number);
        Address second = new(Street, Number);

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Equals_Should_BeFalse_When_ComponentsDiffer()
    {
        Address first = new(Street, Number);
        Address second = new(OtherStreet, Number);

        first.Should().NotBe(second);
    }

    private sealed record Address(string Street, int Number) : ValueObject;
}
