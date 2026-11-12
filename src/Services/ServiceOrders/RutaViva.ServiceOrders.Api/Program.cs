using RutaViva.BuildingBlocks.Api.Correlation;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Serialization;
using RutaViva.ServiceDefaults;
using RutaViva.ServiceOrders.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddApiJsonConventions();
builder.Services.AddServiceOrders(builder.Configuration);

WebApplication app = builder.Build();
app.UseCorrelationId();
app.MapEndpointModules(typeof(Program).Assembly);
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
