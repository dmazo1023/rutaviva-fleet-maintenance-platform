using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class SampleCommandHandler : ICommandHandler<SampleCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(SampleCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
}
