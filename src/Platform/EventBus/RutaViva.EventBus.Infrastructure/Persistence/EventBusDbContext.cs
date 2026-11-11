using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;

namespace RutaViva.EventBus.Infrastructure.Persistence;

public sealed class EventBusDbContext(DbContextOptions<EventBusDbContext> options) : DbContext(options)
{
    public const string Schema = "eventbus";

    public DbSet<StoredEvent> Events => Set<StoredEvent>();

    public DbSet<DeadLetter> DeadLetters => Set<DeadLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new StoredEventConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterConfiguration());
        modelBuilder.ApplyRutaVivaConventions();
    }
}
