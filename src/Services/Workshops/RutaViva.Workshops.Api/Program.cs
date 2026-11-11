using RutaViva.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

WebApplication app = builder.Build();
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
