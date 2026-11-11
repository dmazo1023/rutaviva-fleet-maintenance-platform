using RutaViva.BuildingBlocks.Api.Serialization;
using RutaViva.EventBus.Hub;
using RutaViva.EventBus.Hub.Hubs;
using RutaViva.EventBus.Infrastructure;
using RutaViva.IntegrationContracts.EventBus;
using RutaViva.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddApiJsonConventions();
builder.Services.AddEventBusInfrastructure(builder.Configuration.GetRutaVivaConnectionString());
builder.Services.AddEventBusHandlers();
builder.Services.AddEventBusSignalR();

WebApplication app = builder.Build();
app.MapHub<EventsHub>(EventBusHubMethods.HubPath);
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
