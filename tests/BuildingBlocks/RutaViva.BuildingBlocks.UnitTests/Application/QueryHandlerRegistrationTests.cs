using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Decorators;
using RutaViva.BuildingBlocks.Application.DependencyInjection;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class QueryHandlerRegistrationTests
{
    private const string ValidName = "name";
    private const int SingleRegistration = 1;

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddQueryHandlers(typeof(SampleQueryHandler).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddQueryHandlers_Should_ResolveDecoratedHandler_When_AssemblyContainsHandler()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        IQueryHandler<SampleQuery, string> handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<SampleQuery, string>>();

        handler.Should().BeOfType<LoggingQueryHandlerDecorator<SampleQuery, string>>();
    }

    [Fact]
    public async Task AddQueryHandlers_Should_ReturnValidationError_When_ResolvedHandlerReceivesInvalidQuery()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        IQueryHandler<SampleQuery, string> handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<SampleQuery, string>>();

        Result<string> result = await handler.HandleAsync(new SampleQuery(string.Empty), TestContext.Current.CancellationToken);

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task AddQueryHandlers_Should_ReturnSuccess_When_ResolvedHandlerReceivesValidQuery()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();
        IQueryHandler<SampleQuery, string> handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<SampleQuery, string>>();

        Result<string> result = await handler.HandleAsync(new SampleQuery(ValidName), TestContext.Current.CancellationToken);

        result.Value.Should().Be(ValidName);
    }

    [Fact]
    public void AddQueryHandlers_Should_NotDuplicateValidators_When_CommandHandlersAreRegisteredFirst()
    {
        ServiceCollection services = new();
        services.AddCommandHandlers(typeof(SampleQueryHandler).Assembly);
        services.AddQueryHandlers(typeof(SampleQueryHandler).Assembly);

        int registrations = services.Count(descriptor => descriptor.ServiceType == typeof(IValidator<SampleQuery>));

        registrations.Should().Be(SingleRegistration);
    }
}
