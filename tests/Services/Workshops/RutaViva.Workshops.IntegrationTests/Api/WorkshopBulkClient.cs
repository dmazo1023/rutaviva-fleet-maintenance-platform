using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Workshops.IntegrationTests.Api;

internal static class WorkshopBulkClient
{
    private const string BulkRouteFormat = "/api/v1/workshops/bulk?dryRun={0}";
    private const string OutcomeProperty = "outcome";

    public static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        IReadOnlyList<WorkshopBulkRow> rows,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        BulkRequest<WorkshopBulkRow> request = new(rows);
        string route = string.Format(BulkRouteFormat, dryRun.ToString().ToLowerInvariant());
        return client.PostAsJsonAsync(route, request, cancellationToken);
    }

    public static async Task<IReadOnlyList<string>> ReadOutcomesAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.EnumerateArray()
            .Select(row => row.GetProperty(OutcomeProperty).GetString()!)
            .ToList();
    }
}
