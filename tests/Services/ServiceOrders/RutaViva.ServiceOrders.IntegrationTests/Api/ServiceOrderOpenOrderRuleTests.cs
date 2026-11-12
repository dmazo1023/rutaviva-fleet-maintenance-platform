using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.ServiceOrders.Api.Contracts;
using RutaViva.Testing.Fixtures;

namespace RutaViva.ServiceOrders.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class ServiceOrderOpenOrderRuleTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OrdersRoute = "/api/v1/service-orders";
    private const string CancelRouteFormat = "/api/v1/service-orders/{0}/cancel";
    private const string Operator = "Operador";
    private const string Workshop = "Taller Norte";
    private const string Plate = "TAN202";
    private const string RoutineName = "Cambio de aceite";
    private const string RoutineTypeCode = "OIL_CHANGE";
    private const string IdProperty = "id";
    private const string StatusProperty = "status";
    private const string CodeProperty = "code";
    private const string Cancelled = "Cancelled";
    private const string OpenOrderExistsCode = "ServiceOrder.OpenOrderExists";

    [Fact]
    public async Task CreateOrder_Should_AllowOnlyOneOpenOrderPerRoutine_And_AllowANewOneAfterCancelling()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceOrdersApiFactory factory = await ServiceOrdersApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        CreateServiceOrderRequest request = NewOrder(Guid.CreateVersion7());

        using HttpResponseMessage first = await client.PostAsJsonAsync(OrdersRoute, request, cancellationToken);
        using HttpResponseMessage duplicate = await client.PostAsJsonAsync(OrdersRoute, request, cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetRawText().Should().Contain(OpenOrderExistsCode);

        string id = (await first.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty(IdProperty).GetString()!;
        using HttpResponseMessage cancelled = await client.PostAsJsonAsync(string.Format(CancelRouteFormat, id), new CancelServiceOrderRequest(Operator), cancellationToken);
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancelled.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty(StatusProperty).GetString().Should().Be(Cancelled);

        using HttpResponseMessage again = await client.PostAsJsonAsync(OrdersRoute, request, cancellationToken);
        again.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static CreateServiceOrderRequest NewOrder(Guid routineId) =>
        new(Guid.CreateVersion7(), Plate, Guid.CreateVersion7(), Guid.CreateVersion7(), Workshop, routineId, RoutineName, RoutineTypeCode, Operator);
}
