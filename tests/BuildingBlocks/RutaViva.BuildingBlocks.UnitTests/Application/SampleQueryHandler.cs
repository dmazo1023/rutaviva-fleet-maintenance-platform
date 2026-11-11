using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class SampleQueryHandler : IQueryHandler<SampleQuery, string>
{
    public Task<Result<string>> HandleAsync(SampleQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result<string>.Success(query.Name));
}
