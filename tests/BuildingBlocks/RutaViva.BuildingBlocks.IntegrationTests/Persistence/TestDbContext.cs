using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.IntegrationTests.Persistence;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public const string Schema = "testing";
    public const string OrdersTable = "Orders";

    private const int NameMaxLength = 100;

    public DbSet<TestOrder> Orders => Set<TestOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestOrder>(builder =>
        {
            builder.ToTable(OrdersTable, Schema);
            builder.HasKey(order => order.Id);
            builder.Property(order => order.Id).ValueGeneratedNever();
            builder.Property(order => order.Name).HasMaxLength(NameMaxLength);
        });
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(Schema));
        modelBuilder.ApplyRutaVivaConventions();
    }
}
