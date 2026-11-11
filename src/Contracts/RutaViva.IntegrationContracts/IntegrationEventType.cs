using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RutaViva.IntegrationContracts;

public sealed record IntegrationEventType
{
    public const string Prefix = "rutaviva";

    private const char Separator = '.';
    private const string VersionPrefix = "v";
    private const int SegmentCount = 4;
    private const int PrefixIndex = 0;
    private const int ServiceIndex = 1;
    private const int NameIndex = 2;
    private const int VersionIndex = 3;
    private const int MinimumVersion = 1;

    private IntegrationEventType(string service, string name, int version)
    {
        Service = service;
        Name = name;
        Version = version;
    }

    public string Service { get; }

    public string Name { get; }

    public int Version { get; }

    public static IntegrationEventType Create(string service, string name, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, MinimumVersion);
        return new IntegrationEventType(service, name, version);
    }

    public static IntegrationEventType ForStream(string stream, string name, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stream);
        return Create(stream.Split(Separator)[PrefixIndex], name, version);
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out IntegrationEventType? eventType)
    {
        eventType = Parse(value);
        return eventType is not null;
    }

    public override string ToString() =>
        string.Join(Separator, Prefix, Service, Name, VersionPrefix + Version.ToString(CultureInfo.InvariantCulture));

    private static IntegrationEventType? Parse(string? value)
    {
        string[] segments = value?.Split(Separator) ?? [];
        if (segments.Length != SegmentCount || segments[PrefixIndex] != Prefix)
        {
            return null;
        }

        int? version = ParseVersion(segments[VersionIndex]);
        bool isValid = version >= MinimumVersion
            && !string.IsNullOrWhiteSpace(segments[ServiceIndex])
            && !string.IsNullOrWhiteSpace(segments[NameIndex]);
        return isValid ? new IntegrationEventType(segments[ServiceIndex], segments[NameIndex], version!.Value) : null;
    }

    private static int? ParseVersion(string segment) =>
        segment.StartsWith(VersionPrefix, StringComparison.Ordinal)
        && int.TryParse(segment.AsSpan(VersionPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int version)
            ? version
            : null;
}
