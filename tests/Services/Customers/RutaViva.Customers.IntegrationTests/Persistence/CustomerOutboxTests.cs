using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Infrastructure.DependencyInjection;
using RutaViva.Customers.Infrastructure.Persistence;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class CustomerOutboxTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ConnectionStringKey = "ConnectionStrings:RutaViva";
    private const string CustomerName = "Acme Transport";
    private const string CustomerTaxId = "900300100-1";
    private const string CustomerEmail = "customer@acme.test";
    private const string CustomerPhone = "+573001234567";
    private const string DuplicateTaxId = "900300200-2";
    private const string DuplicateErrorCode = "Customer.TaxIdAlreadyRegistered";

    [Fact]
    public async Task HandleAsync_Should_WriteCustomerRegisteredToOutbox_When_CustomerIsRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersDbContext schemaContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ICommandHandler<RegisterCustomerCommand, Guid> handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>();

        Result<Guid> registered = await handler.HandleAsync(
            new RegisterCustomerCommand(CustomerName, CustomerTaxId, CustomerEmail, CustomerPhone),
            cancellationToken);

        registered.IsSuccess.Should().BeTrue();
        string subject = registered.Value.ToString();
        List<OutboxMessage> messages = await schemaContext.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(stored => stored.Subject == subject)
            .ToListAsync(cancellationToken);
        OutboxMessage message = messages.Should().ContainSingle().Subject;
        message.Stream.Should().Be(Streams.CustomersCustomer);
        message.EventType.Should().Contain(CustomerRegisteredV1.EventName);
        message.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTaxIdAlreadyRegistered_When_TaxIdWasAlreadyRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using CustomersDbContext schemaContext = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ICommandHandler<RegisterCustomerCommand, Guid> handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<RegisterCustomerCommand, Guid>>();
        RegisterCustomerCommand command = new(CustomerName, DuplicateTaxId, CustomerEmail, CustomerPhone);
        await handler.HandleAsync(command, cancellationToken);

        Result<Guid> second = await handler.HandleAsync(command, cancellationToken);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(DuplicateErrorCode);
    }

    private ServiceProvider BuildProvider()
    {
        Dictionary<string, string?> settings = new() { [ConnectionStringKey] = database.ConnectionString };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddCustomers(configuration);
        return services.BuildServiceProvider();
    }
}
