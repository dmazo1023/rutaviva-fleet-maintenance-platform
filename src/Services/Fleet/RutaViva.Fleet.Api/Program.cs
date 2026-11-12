using RutaViva.BuildingBlocks.Api.Correlation;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Serialization;
using RutaViva.Fleet.Api.Serialization;
using RutaViva.Fleet.Infrastructure.DependencyInjection;
using RutaViva.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddApiJsonConventions();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new FleetIdJsonConverterFactory()));
builder.Services.AddFleet(builder.Configuration);

WebApplication app = builder.Build();
app.UseCorrelationId();
app.MapEndpointModules(typeof(Program).Assembly);
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
