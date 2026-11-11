using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RutaViva.BuildingBlocks.Api.Correlation;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    private const string ScopeKey = "CorrelationId";
    private const int MaxLength = 128;
    private const string GuidFormat = "N";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context.Request);
        context.Response.Headers[HeaderName] = correlationId;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object> { [ScopeKey] = correlationId });
        await next(context);
    }

    private static string ResolveCorrelationId(HttpRequest request)
    {
        string? received = request.Headers[HeaderName].FirstOrDefault();
        return IsUsable(received) ? received! : Guid.NewGuid().ToString(GuidFormat);
    }

    private static bool IsUsable(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= MaxLength;
    }
}
