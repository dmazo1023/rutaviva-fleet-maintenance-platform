using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.ServiceOrders.Api.Contracts;
using RutaViva.ServiceOrders.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.ServiceOrders.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class ServiceOrderHappyPathTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OrdersRoute = "/api/v1/service-orders";
    private const string ActionRouteFormat = "/api/v1/service-orders/{0}/{1}";
    private const string ApproverDetailFormat = "/api/v1/service-orders/{0}?actor=Approver";
    private const string PendingApprovalListRoute = "/api/v1/service-orders?statusGroup=PendingApproval";
    private const string Workshop = "Taller Norte";
    private const string Approver = "Aprobador";
    private const string Operator = "Operador";
    private const decimal HighAmount = 1_500_000m;
    private const long ServicedKm = 45_200;
    private const string StatusProperty = "status";
    private const string IdProperty = "id";
    private const string ActionsProperty = "availableActions";
    private const string ActionProperty = "action";
    private const string HistoryProperty = "history";
    private const string PendingApproval = "PendingApproval";
    private const string Completed = "Completed";
    private const string Approve = "Approve";
    private const int CompletedHistoryEntries = 6;

    [Fact]
    public async Task CompleteOrder_Should_WriteServiceOrderCompletedToOutbox_When_HighQuoteIsApproved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceOrdersApiFactory factory = await ServiceOrdersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        ServiceOrderTestReferences references = await ServiceOrderTestReferences.SeedAsync(database, cancellationToken);

        using HttpResponseMessage created = await client.PostAsJsonAsync(OrdersRoute, new CreateServiceOrderRequest(references.VehicleId, references.RoutineId, Operator), cancellationToken);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        string id = (await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty(IdProperty).GetString()!;

        JsonElement quoted = await PostAsync(client, id, "quote", new QuoteServiceOrderRequest(HighAmount, Workshop), cancellationToken);
        quoted.GetProperty(StatusProperty).GetString().Should().Be(PendingApproval);
        JsonElement pending = await client.GetFromJsonAsync<JsonElement>(PendingApprovalListRoute, cancellationToken);
        pending.EnumerateArray().Select(item => item.GetProperty(IdProperty).GetString()).Should().Contain(id);
        JsonElement forApprover = await client.GetFromJsonAsync<JsonElement>(string.Format(ApproverDetailFormat, id), cancellationToken);
        forApprover.GetProperty(ActionsProperty).EnumerateArray().Select(item => item.GetProperty(ActionProperty).GetString()).Should().Contain(Approve);

        await PostAsync(client, id, "approve", new ApproveServiceOrderRequest(Approver, references.ApproverId), cancellationToken);
        await PostAsync(client, id, "start", new StartServiceOrderRequest(Workshop), cancellationToken);
        JsonElement completed = await PostAsync(client, id, "complete", new CompleteServiceOrderRequest(Workshop, ServicedKm), cancellationToken);

        completed.GetProperty(StatusProperty).GetString().Should().Be(Completed);
        completed.GetProperty(HistoryProperty).GetArrayLength().Should().Be(CompletedHistoryEntries);
        await using ServiceOrdersDbContext reader = await database.CreateDbContextAsync<ServiceOrdersDbContext>(cancellationToken);
        List<OutboxMessage> messages = await reader.Set<OutboxMessage>().ToListAsync(cancellationToken);
        messages.Should().ContainSingle(message => message.EventType.Contains(ServiceOrderCompletedV1.EventName));
    }

    private static async Task<JsonElement> PostAsync<TRequest>(HttpClient client, string id, string action, TRequest request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(string.Format(ActionRouteFormat, id, action), request, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
