using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Application.Workshops;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Infrastructure.Persistence;
using RutaViva.Workshops.Infrastructure.Persistence.Queries;

namespace RutaViva.Workshops.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class WorkshopQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string CityStoredValue = "Barranquilla";
    private const string CityFilterValue = "  bARRANQUILLA ";
    private const string OtherCity = "Pereira";
    private const string FirstTaxId = "900300001";
    private const string SecondTaxId = "900300002";
    private const string ThirdTaxId = "900300003";
    private const string StatusCity = "Manizales";
    private const string ActiveTaxId = "900300004";
    private const string InactiveTaxId = "900300005";
    private const string DetailCity = "Cartagena";
    private const string DetailTaxId = "900300006";
    private const string DetailName = "Taller Caribe";
    private const string UnknownCity = "Leticia";

    [Fact]
    public async Task ListAsync_Should_FilterByCityIgnoringCase_When_CityIsProvided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop first = WorkshopTestData.Create(FirstTaxId, CityStoredValue);
        Workshop second = WorkshopTestData.Create(SecondTaxId, CityStoredValue);
        Workshop other = WorkshopTestData.Create(ThirdTaxId, OtherCity);
        await SeedAsync([first, second, other], cancellationToken);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        IReadOnlyList<WorkshopSummaryDto> result = await new WorkshopQueries(context).ListAsync(null, CityFilterValue, cancellationToken);

        result.Select(item => item.Id).Should().BeEquivalentTo([first.Id.Value, second.Id.Value]);
        result.Should().OnlyContain(item => item.City == CityStoredValue);
    }

    [Fact]
    public async Task ListAsync_Should_FilterByStatus_When_StatusIsProvided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop active = WorkshopTestData.Create(ActiveTaxId, StatusCity);
        Workshop inactive = WorkshopTestData.Create(InactiveTaxId, StatusCity);
        inactive.Deactivate();
        await SeedAsync([active, inactive], cancellationToken);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        IReadOnlyList<WorkshopSummaryDto> result = await new WorkshopQueries(context)
            .ListAsync(WorkshopStatus.Inactive, StatusCity, cancellationToken);

        result.Should().ContainSingle().Which.Id.Should().Be(inactive.Id.Value);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnEmpty_When_NoWorkshopMatchesCity()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        IReadOnlyList<WorkshopSummaryDto> result = await new WorkshopQueries(context).ListAsync(null, UnknownCity, cancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailAsync_Should_ReturnProjectedDetail_When_WorkshopExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Workshop workshop = WorkshopTestData.Create(DetailTaxId, DetailCity, DetailName, null);
        await SeedAsync([workshop], cancellationToken);
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);

        WorkshopDetailDto? detail = await new WorkshopQueries(context).GetDetailAsync(workshop.Id, cancellationToken);

        detail.Should().NotBeNull();
        detail!.Id.Should().Be(workshop.Id.Value);
        detail.Name.Should().Be(DetailName);
        detail.TaxId.Should().Be(DetailTaxId);
        detail.Email.Should().Be(WorkshopTestData.DefaultEmail);
        detail.Phone.Should().BeNull();
        detail.Address.Should().Be(WorkshopTestData.DefaultAddress);
        detail.City.Should().Be(DetailCity);
        detail.Status.Should().Be(WorkshopStatus.Active);
    }

    private async Task SeedAsync(IReadOnlyList<Workshop> workshops, CancellationToken cancellationToken)
    {
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        context.Workshops.AddRange(workshops);
        await context.SaveChangesAsync(cancellationToken);
    }
}
