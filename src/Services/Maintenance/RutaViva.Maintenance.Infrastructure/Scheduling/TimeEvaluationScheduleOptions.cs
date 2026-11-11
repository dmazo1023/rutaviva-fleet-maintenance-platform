using System.Globalization;
using Microsoft.Extensions.Configuration;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Scheduling;

public sealed class TimeEvaluationScheduleOptions
{
    public const string SectionName = "Maintenance:TimeEvaluation";
    public const string DefaultTimeZoneId = MaintenanceTimeZone.BogotaId;

    private const string RunAtKey = "RunAt";
    private const string TimeZoneIdKey = "TimeZoneId";
    private const int DefaultRunAtHour = 0;
    private const int DefaultRunAtMinute = 5;

    public static readonly TimeOnly DefaultRunAt = new(DefaultRunAtHour, DefaultRunAtMinute);

    public TimeOnly RunAt { get; init; } = DefaultRunAt;

    public string TimeZoneId { get; init; } = DefaultTimeZoneId;

    public static TimeEvaluationScheduleOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IConfigurationSection section = configuration.GetSection(SectionName);
        return new TimeEvaluationScheduleOptions
        {
            RunAt = ParseRunAt(section[RunAtKey]),
            TimeZoneId = ResolveTimeZoneId(section[TimeZoneIdKey]),
        };
    }

    private static TimeOnly ParseRunAt(string? value) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsed)
            ? parsed
            : DefaultRunAt;

    private static string ResolveTimeZoneId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultTimeZoneId : value;
}
