using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;
using RutaViva.Workshops.Infrastructure.Persistence;
using RutaViva.Workshops.Infrastructure.Persistence.Repositories;

namespace RutaViva.Workshops.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class WorkshopRepositoryTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string StoredTaxId = "900400001";
    private const string FreeTaxId = "900400002";
    private const string TakenTaxId = "900400003";
    private const string RepositoryCity = "Ibague";

    [Fact]
    public async Task GetByTaxIdAsync_Should_ReturnWorkshopWithAddress_When_TaxIdExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop workshop = await SeedAsync(StoredTaxId, cancellationToken);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        Workshop? found = await new WorkshopRepository(context).GetByTaxIdAsync(TaxId.Create(StoredTaxId).Value, cancellationToken);

        found.Should().NotBeNull();
        found!.Id.Should().Be(workshop.Id);
        found.Address.City.Should().Be(RepositoryCity);
        found.Address.Address.Should().Be(WorkshopTestData.DefaultAddress);
        found.Phone.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_WorkshopDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        Workshop? found = await new WorkshopRepository(context).GetByIdAsync(new(Guid.NewGuid()), cancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    public async Task IsTaxIdTakenAsync_Should_ReflectStoredTaxIds_When_Queried()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(TakenTaxId, cancellationToken);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        WorkshopUniquenessChecker checker = new(context);

        bool taken = await checker.IsTaxIdTakenAsync(TaxId.Create(TakenTaxId).Value, cancellationToken);
        bool free = await checker.IsTaxIdTakenAsync(TaxId.Create(FreeTaxId).Value, cancellationToken);

        taken.Should().BeTrue();
        free.Should().BeFalse();
    }

    private async Task<Workshop> SeedAsync(string taxId, CancellationToken cancellationToken)
    {
        Workshop workshop = WorkshopTestData.Create(taxId, RepositoryCity);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        new WorkshopRepository(context).Add(workshop);
        await context.SaveChangesAsync(cancellationToken);
        return workshop;
    }
}
