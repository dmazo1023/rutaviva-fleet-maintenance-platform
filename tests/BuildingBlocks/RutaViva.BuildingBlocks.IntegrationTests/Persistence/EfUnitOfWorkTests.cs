using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class EfUnitOfWorkTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ConflictCode = "Concurrency.Conflict";
    private const string FirstName = "first";
    private const string SecondName = "second";

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnSuccess_When_NoConflictExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        TestOrder order = new TestOrderBuilder().Build();
        context.Orders.Add(order);
        EfUnitOfWork<TestDbContext> unitOfWork = new(context);

        Result result = await unitOfWork.SaveChangesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        order.RowVersion.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnConflict_When_RowVersionIsStale()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TestOrder seed = new TestOrderBuilder().Build();
        await SeedAsync(seed, cancellationToken);
        await using TestDbContext firstContext = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        await using TestDbContext secondContext = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        TestOrder firstOrder = await firstContext.Orders.SingleAsync(order => order.Id == seed.Id, cancellationToken);
        TestOrder secondOrder = await secondContext.Orders.SingleAsync(order => order.Id == seed.Id, cancellationToken);
        firstOrder.Rename(FirstName);
        secondOrder.Rename(SecondName);

        Result firstResult = await new EfUnitOfWork<TestDbContext>(firstContext).SaveChangesAsync(cancellationToken);
        Result secondResult = await new EfUnitOfWork<TestDbContext>(secondContext).SaveChangesAsync(cancellationToken);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Type.Should().Be(ErrorType.Conflict);
        secondResult.Error.Code.Should().Be(ConflictCode);
    }

    private async Task SeedAsync(TestOrder order, CancellationToken cancellationToken)
    {
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        context.Orders.Add(order);
        Result result = await new EfUnitOfWork<TestDbContext>(context).SaveChangesAsync(cancellationToken);
        result.IsSuccess.Should().BeTrue();
    }
}
