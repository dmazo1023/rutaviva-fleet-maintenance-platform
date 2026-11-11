using Microsoft.Extensions.Logging;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<CapturedLog> _entries = [];

    public IReadOnlyList<CapturedLog> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _entries.Add(new CapturedLog(logLevel, formatter(state, exception), exception));
}
