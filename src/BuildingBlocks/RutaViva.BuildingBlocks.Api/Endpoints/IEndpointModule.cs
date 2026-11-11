using Microsoft.AspNetCore.Routing;

namespace RutaViva.BuildingBlocks.Api.Endpoints;

public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
