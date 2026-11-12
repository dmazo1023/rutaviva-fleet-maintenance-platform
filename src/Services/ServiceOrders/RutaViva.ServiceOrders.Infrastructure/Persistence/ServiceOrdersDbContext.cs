using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;
using RutaViva.ServiceOrders.Domain.References;
using RutaViva.ServiceOrders.Infrastructure.Persistence.Configurations;
using RutaViva.ServiceOrders.Infrastructure.Persistence.Converters;

namespace RutaViva.ServiceOrders.Infrastructure.Persistence;

public sealed class ServiceOrdersDbContext(DbContextOptions<ServiceOrdersDbContext> options) : DbContext(options)
{
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    public DbSet<VehicleReference> VehicleReferences => Set<VehicleReference>();

    public DbSet<WorkshopReference> WorkshopReferences => Set<WorkshopReference>();

    public DbSet<RoutineReference> RoutineReferences => Set<RoutineReference>();

    public DbSet<ApproverReference> ApproverReferences => Set<ApproverReference>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ServiceOrderId>().HaveConversion<ServiceOrderIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ServiceOrdersSchema.Name);
        modelBuilder.ApplyConfiguration(new ServiceOrderConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new WorkshopReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new RoutineReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new ApproverReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(ServiceOrdersSchema.Name));
        modelBuilder.ApplyConfiguration(new EventCheckpointConfiguration(ServiceOrdersSchema.Name));
        modelBuilder.ApplyRutaVivaConventions();
        modelBuilder.ApplyEnumCheckConstraints();
    }
}
