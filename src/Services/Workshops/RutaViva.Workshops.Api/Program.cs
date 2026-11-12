using RutaViva.BuildingBlocks.Api.Correlation;
using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Serialization;
using RutaViva.ServiceDefaults;
using RutaViva.Workshops.Application.Bulk;
using RutaViva.Workshops.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddApiJsonConventions();
builder.Services.AddWorkshops(builder.Configuration);
builder.Services.AddScoped<WorkshopBulkRegistrationHandlerFactory>();

WebApplication app = builder.Build();
app.UseCorrelationId();
app.MapEndpointModules(typeof(Program).Assembly);
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
