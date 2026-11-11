namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly TodayIn(TimeZoneInfo timeZone);
}
