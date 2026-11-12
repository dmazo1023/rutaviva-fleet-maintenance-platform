using System.Net.Http.Json;
using System.Text.Json;

namespace RutaViva.Fleet.IntegrationTests.Api;

internal static class BulkResponseReader
{
    private const string OutcomeProperty = "outcome";
    private const string ErrorsProperty = "errors";
    private const string CodeProperty = "code";

    public static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

    public static IReadOnlyList<string?> Outcomes(JsonElement body) =>
        body.EnumerateArray().Select(row => row.GetProperty(OutcomeProperty).GetString()).ToList();

    public static IReadOnlyList<string?> FirstErrorCodes(JsonElement body) =>
        body.EnumerateArray()
            .Select(row => row.GetProperty(ErrorsProperty))
            .Select(errors => errors.GetArrayLength() == 0 ? null : errors[0].GetProperty(CodeProperty).GetString())
            .ToList();
}
