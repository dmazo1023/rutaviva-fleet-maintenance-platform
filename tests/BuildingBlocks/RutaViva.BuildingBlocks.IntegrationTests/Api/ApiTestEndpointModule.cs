using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.IntegrationTests.Api;

internal sealed class ApiTestEndpointModule : IEndpointModule
{
    public const string ValidationRoute = "/test/validation";
    public const string NotFoundRoute = "/test/not-found";
    public const string ConflictRoute = "/test/conflict";
    public const string BusinessRuleRoute = "/test/business-rule";
    public const string SuccessRoute = "/test/success";
    public const string NoContentRoute = "/test/no-content";
    public const string LogRoute = "/test/log";
    public const string EnumRoute = "/test/enum";
    public const string ValidationCode = "Test.Validation";
    public const string NotFoundCode = "Test.NotFound";
    public const string ConflictCode = "Concurrency.Conflict";
    public const string BusinessRuleCode = "ServiceOrder.InvalidTransition";
    public const string ErrorMessage = "Readable detail";
    public const string SuccessValue = "ok";

    private const int LogEventId = 1;
    private const string LogMessage = "Test request handled";

    private static readonly Action<ILogger, Exception?> LogRequest =
        LoggerMessage.Define(LogLevel.Information, new EventId(LogEventId), LogMessage);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ValidationRoute, () => Fail(ValidationCode, ErrorType.Validation));
        app.MapGet(NotFoundRoute, () => Fail(NotFoundCode, ErrorType.NotFound));
        app.MapGet(ConflictRoute, () => Fail(ConflictCode, ErrorType.Conflict));
        app.MapGet(BusinessRuleRoute, () => Fail(BusinessRuleCode, ErrorType.BusinessRule));
        app.MapGet(SuccessRoute, () => Result<string>.Success(SuccessValue).ToHttpResult());
        app.MapGet(NoContentRoute, () => Result.Success().ToHttpResult());
        app.MapGet(LogRoute, (ILoggerFactory factory) => Log(factory));
        app.MapPost(EnumRoute, (ApiTestRequest request) => Results.Ok(request));
    }

    private static IResult Fail(string code, ErrorType type)
    {
        return Result<string>.Failure(new Error(code, ErrorMessage, type)).ToHttpResult();
    }

    private static IResult Log(ILoggerFactory factory)
    {
        LogRequest(factory.CreateLogger<ApiTestEndpointModule>(), null);
        return Results.Ok();
    }
}
