using RutaViva.BuildingBlocks.Api.Correlation;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Serialization;
using RutaViva.Customers.Infrastructure.DependencyInjection;
using RutaViva.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddApiJsonConventions();
builder.Services.AddCustomers(builder.Configuration);

WebApplication app = builder.Build();
app.UseCorrelationId();
app.MapEndpointModules(typeof(Program).Assembly);
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
