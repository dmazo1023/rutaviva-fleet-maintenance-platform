using Microsoft.Extensions.Logging;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

internal sealed record CapturedLog(LogLevel Level, string Message, Exception? Exception);
