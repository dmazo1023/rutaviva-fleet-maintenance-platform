using Microsoft.EntityFrameworkCore.Metadata;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class RutaVivaConventionsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OutboxTable = "OutboxMessages";
    private const string CheckpointTable = "EventCheckpoints";
    private const string ExpectedFilter = "([ProcessedAt] IS NULL AND [FailedAt] IS NULL)";

    [Theory]
    [InlineData(TestDbContext.OrdersTable, "Status", "varchar(20)")]
    [InlineData(TestDbContext.OrdersTable, "CreatedAt", "datetimeoffset(0)")]
    [InlineData(TestDbContext.OrdersTable, "Amount", "decimal(18,2)")]
    [InlineData(TestDbContext.OrdersTable, "RowVersion", "timestamp")]
    [InlineData(OutboxTable, "OccurredAt", "datetimeoffset(0)")]
    [InlineData(OutboxTable, "Stream", "varchar(60)")]
    [InlineData(OutboxTable, "EventType", "varchar(150)")]
    [InlineData(OutboxTable, "Subject", "varchar(60)")]
    [InlineData(OutboxTable, "Payload", "nvarchar(max)")]
    [InlineData(OutboxTable, "LastError", "nvarchar(1000)")]
    [InlineData(CheckpointTable, "SubscriberName", "varchar(60)")]
    [InlineData(CheckpointTable, "LastOffset", "bigint")]
    [InlineData(CheckpointTable, "UpdatedAt", "datetimeoffset(0)")]
    public async Task Columns_Should_UseExpectedStoreType_When_ConventionsAreApplied(string table, string column, string expected)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);

        string? actual = await SqlSchemaReader.ReadColumnTypeAsync(database.ConnectionString, TestDbContext.Schema, table, column, cancellationToken);

        actual.Should().Be(expected);
    }

    [Fact]
    public async Task RowVersion_Should_BeShadowProperty_When_EntityIsAggregateRoot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);

        IProperty property = context.Model.FindEntityType(typeof(TestOrder))!.FindProperty(RutaVivaModelBuilderExtensions.RowVersionPropertyName)!;

        typeof(TestOrder).GetProperty(RutaVivaModelBuilderExtensions.RowVersionPropertyName).Should().BeNull();
        property.IsShadowProperty().Should().BeTrue();
        property.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public async Task OutboxIndex_Should_BeFilteredToPendingMessages_When_ModelIsCreated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);

        string? filter = await SqlSchemaReader.ReadIndexFilterAsync(database.ConnectionString, TestDbContext.Schema, OutboxTable, cancellationToken);

        filter.Should().Be(ExpectedFilter);
    }
}
