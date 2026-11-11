using RutaViva.BuildingBlocks.Infrastructure.Messaging;

namespace RutaViva.BuildingBlocks.IntegrationTests.Messaging;

public sealed class EventCheckpointTests
{
    private const string SubscriberName = "subscriber";
    private const long CurrentOffset = 50;
    private const long LowerOffset = 49;
    private const long HigherOffset = 51;

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void MoveTo_Should_Throw_When_OffsetIsLowerThanCurrent()
    {
        EventCheckpoint checkpoint = new(SubscriberName, CurrentOffset, Now);

        Action act = () => checkpoint.MoveTo(LowerOffset, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
        checkpoint.LastOffset.Should().Be(CurrentOffset);
    }

    [Theory]
    [InlineData(CurrentOffset)]
    [InlineData(HigherOffset)]
    public void MoveTo_Should_UpdateOffset_When_OffsetIsNotLowerThanCurrent(long offset)
    {
        EventCheckpoint checkpoint = new(SubscriberName, CurrentOffset, Now);

        checkpoint.MoveTo(offset, Now);

        checkpoint.LastOffset.Should().Be(offset);
    }
}
