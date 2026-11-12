using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.IntegrationTests.Api;

internal static class BulkTestClient
{
    public const string CustomersBulkRoute = "/api/v1/customers/bulk";
    public const string ApproversBulkRoute = "/api/v1/customers/approvers/bulk";

    private const string DryRunQuery = "?dryRun=true";

    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    public static async Task<HttpResponseMessage> PostAsync<TRow>(
        HttpClient client,
        string route,
        IReadOnlyList<TRow> rows,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        string target = dryRun ? route + DryRunQuery : route;
        return await client.PostAsJsonAsync(target, new BulkRequest<TRow>(rows), JsonOptions, cancellationToken);
    }

    public static async Task<IReadOnlyList<BulkRowResult>> PostForResultsAsync<TRow>(
        HttpClient client,
        string route,
        IReadOnlyList<TRow> rows,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await PostAsync(client, route, rows, dryRun, cancellationToken);
        response.EnsureSuccessStatusCode();
        List<BulkRowResult>? results = await response.Content.ReadFromJsonAsync<List<BulkRowResult>>(JsonOptions, cancellationToken);
        return results!;
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
