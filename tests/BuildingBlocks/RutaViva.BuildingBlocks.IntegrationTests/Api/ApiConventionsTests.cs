using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RutaViva.BuildingBlocks.Api.Correlation;

namespace RutaViva.BuildingBlocks.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class ApiConventionsTests(ApiTestHost host) : IClassFixture<ApiTestHost>
{
    private const string ProblemJson = "application/problem+json";
    private const string CodeProperty = "code";
    private const string DetailProperty = "detail";
    private const string StatusProperty = "status";
    private const string ReceivedCorrelationId = "abc-123";
    private const string ScopeKey = "CorrelationId";
    private const string JsonContentType = "application/json";
    private const string UnknownEnumBody = "{\"status\":\"Paused\"}";
    private const string IntegerEnumBody = "{\"status\":1}";
    private const string KnownEnumBody = "{\"status\":\"Active\"}";

    [Theory]
    [InlineData(ApiTestEndpointModule.ValidationRoute, HttpStatusCode.BadRequest, ApiTestEndpointModule.ValidationCode)]
    [InlineData(ApiTestEndpointModule.NotFoundRoute, HttpStatusCode.NotFound, ApiTestEndpointModule.NotFoundCode)]
    [InlineData(ApiTestEndpointModule.ConflictRoute, HttpStatusCode.Conflict, ApiTestEndpointModule.ConflictCode)]
    [InlineData(ApiTestEndpointModule.BusinessRuleRoute, HttpStatusCode.UnprocessableEntity, ApiTestEndpointModule.BusinessRuleCode)]
    public async Task ToHttpResult_Should_ReturnProblemDetailsWithCode_When_ResultFails(
        string route,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await host.Client.GetAsync(route, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        body.GetProperty(CodeProperty).GetString().Should().Be(expectedCode);
        body.GetProperty(DetailProperty).GetString().Should().Be(ApiTestEndpointModule.ErrorMessage);
        body.GetProperty(StatusProperty).GetInt32().Should().Be((int)expectedStatus);
    }

    [Fact]
    public async Task ToHttpResult_Should_ReturnOkWithValue_When_ResultSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await host.Client.GetAsync(ApiTestEndpointModule.SuccessRoute, cancellationToken);
        string value = await response.Content.ReadFromJsonAsync<string>(cancellationToken) ?? string.Empty;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        value.Should().Be(ApiTestEndpointModule.SuccessValue);
    }

    [Fact]
    public async Task ToHttpResult_Should_ReturnNoContent_When_NonGenericResultSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await host.Client.GetAsync(ApiTestEndpointModule.NoContentRoute, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_Should_EchoHeader_When_RequestProvidesIt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpRequestMessage request = new(HttpMethod.Get, ApiTestEndpointModule.SuccessRoute);
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, ReceivedCorrelationId);

        HttpResponseMessage response = await host.Client.SendAsync(request, cancellationToken);

        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single().Should().Be(ReceivedCorrelationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_Should_GenerateHeader_When_RequestOmitsIt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await host.Client.GetAsync(ApiTestEndpointModule.SuccessRoute, cancellationToken);

        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_Should_IncludeIdInLogScope_When_RequestLogs()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string correlationId = Guid.NewGuid().ToString();
        using HttpRequestMessage request = new(HttpMethod.Get, ApiTestEndpointModule.LogRoute);
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        await host.Client.SendAsync(request, cancellationToken);

        host.Logs.Entries.Should().Contain(scopes => scopes.Any(scope => HasCorrelationId(scope, correlationId)));
    }

    [Fact]
    public async Task Json_Should_ReturnBadRequest_When_EnumValueIsUnknown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await PostEnumAsync(UnknownEnumBody, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Json_Should_ReturnBadRequest_When_EnumValueIsInteger()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await PostEnumAsync(IntegerEnumBody, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Json_Should_SerializeEnumAsName_When_EnumValueIsKnown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await PostEnumAsync(KnownEnumBody, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(nameof(ApiTestStatus.Active));
    }

    private static bool HasCorrelationId(object? scope, string correlationId)
    {
        return scope is IEnumerable<KeyValuePair<string, object>> pairs
            && pairs.Any(pair => pair.Key == ScopeKey && Equals(pair.Value, correlationId));
    }

    private Task<HttpResponseMessage> PostEnumAsync(string body, CancellationToken cancellationToken)
    {
        StringContent content = new(body, Encoding.UTF8, JsonContentType);
        return host.Client.PostAsync(ApiTestEndpointModule.EnumRoute, content, cancellationToken);
    }
}
