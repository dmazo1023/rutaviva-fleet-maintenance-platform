using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class EventsTestDbContext(DbContextOptions<EventsTestDbContext> options) : DbContext(options)
{
    public const string Schema = "events";

    public DbSet<TestParent> Parents => Set<TestParent>();

    public DbSet<TestChild> Children => Set<TestChild>();

    public DbSet<TestLoop> Loops => Set<TestLoop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureAggregate<TestParent>(modelBuilder, nameof(Parents));
        ConfigureAggregate<TestChild>(modelBuilder, nameof(Children));
        ConfigureAggregate<TestLoop>(modelBuilder, nameof(Loops));
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(Schema));
        modelBuilder.ApplyRutaVivaConventions();
    }

    private static void ConfigureAggregate<TAggregate>(ModelBuilder modelBuilder, string table)
        where TAggregate : AggregateRoot<Guid>
    {
        modelBuilder.Entity<TAggregate>(builder =>
        {
            builder.ToTable(table, Schema);
            builder.HasKey(aggregate => aggregate.Id);
            builder.Property(aggregate => aggregate.Id).ValueGeneratedNever();
        });
    }
}
