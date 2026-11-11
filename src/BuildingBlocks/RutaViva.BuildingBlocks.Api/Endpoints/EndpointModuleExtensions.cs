using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace RutaViva.BuildingBlocks.Api.Endpoints;

public static class EndpointModuleExtensions
{
    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder app, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (IEndpointModule module in DiscoverModules(assembly))
        {
            module.MapEndpoints(app);
        }

        return app;
    }

    private static IEnumerable<IEndpointModule> DiscoverModules(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(IsConcreteModule)
            .Select(type => (IEndpointModule)Activator.CreateInstance(type)!)
            .ToList();
    }

    private static bool IsConcreteModule(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } && typeof(IEndpointModule).IsAssignableFrom(type);
    }
}
