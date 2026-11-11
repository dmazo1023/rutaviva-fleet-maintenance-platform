using Microsoft.Extensions.Logging;

namespace RutaViva.BuildingBlocks.IntegrationTests.Api;

internal sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<IReadOnlyCollection<object?>> _entries = [];
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public IReadOnlyCollection<IReadOnlyCollection<object?>> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public void Dispose()
    {
    }

    private void Capture()
    {
        List<object?> scopes = [];
        _scopeProvider.ForEachScope((scope, list) => list.Add(scope), scopes);
        lock (_entries)
        {
            _entries.Add(scopes);
        }
    }

    private IDisposable? Push<TState>(TState state)
        where TState : notnull
    {
        return _scopeProvider.Push(state);
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return provider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            provider.Capture();
        }
    }
}
