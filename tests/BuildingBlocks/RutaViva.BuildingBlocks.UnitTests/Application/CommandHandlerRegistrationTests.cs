using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class CommandHandlerRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddCommandHandlers(typeof(SampleCommandHandler).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddCommandHandlers_Should_ResolveDecoratedHandler_When_AssemblyContainsHandler()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        ICommandHandler<SampleCommand, Guid> handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SampleCommand, Guid>>();

        handler.Should().BeOfType<LoggingCommandHandlerDecorator<SampleCommand, Guid>>();
    }

    [Fact]
    public async Task AddCommandHandlers_Should_ReturnValidationError_When_ResolvedHandlerReceivesInvalidCommand()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        ICommandHandler<SampleCommand, Guid> handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SampleCommand, Guid>>();

        Result<Guid> result = await handler.HandleAsync(new SampleCommand(string.Empty), TestContext.Current.CancellationToken);

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task AddCommandHandlers_Should_ReturnSuccess_When_ResolvedHandlerReceivesValidCommand()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        ICommandHandler<SampleCommand, Guid> handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SampleCommand, Guid>>();

        Result<Guid> result = await handler.HandleAsync(new SampleCommand("name"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
