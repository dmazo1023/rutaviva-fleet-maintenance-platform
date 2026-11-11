using RutaViva.BuildingBlocks.Domain.Guards;

namespace RutaViva.BuildingBlocks.UnitTests.Domain;

public sealed class GuardTests
{
    private const string Text = "value";
    private const int PositiveValue = 5;
    private const int NegativeValue = -1;

    [Fact]
    public void NotNull_Should_ReturnValue_When_ValueIsNotNull()
    {
        object value = new();

        Guard.NotNull(value).Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_Should_Throw_When_ValueIsNull()
    {
        Action act = () => Guard.NotNull<object>(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrWhiteSpace_Should_ReturnValue_When_ValueHasText()
    {
        Guard.NotNullOrWhiteSpace(Text).Should().Be(Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_Should_Throw_When_ValueIsBlank(string? value)
    {
        Action act = () => Guard.NotNullOrWhiteSpace(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotDefault_Should_ReturnValue_When_ValueIsNotDefault()
    {
        Guid value = Guid.NewGuid();

        Guard.NotDefault(value).Should().Be(value);
    }

    [Fact]
    public void NotDefault_Should_Throw_When_ValueIsDefault()
    {
        Action act = () => Guard.NotDefault(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Positive_Should_ReturnValue_When_ValueIsGreaterThanZero()
    {
        Guard.Positive(PositiveValue).Should().Be(PositiveValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(NegativeValue)]
    public void Positive_Should_Throw_When_ValueIsNotPositive(int value)
    {
        Action act = () => Guard.Positive(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
