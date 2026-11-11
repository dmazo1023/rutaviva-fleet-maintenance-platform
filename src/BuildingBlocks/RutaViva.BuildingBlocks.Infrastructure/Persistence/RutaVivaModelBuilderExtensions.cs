using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace RutaViva.BuildingBlocks.Infrastructure.Persistence;

public static class RutaVivaModelBuilderExtensions
{
    private const int EnumMaxLength = 20;
    private const int DateTimeOffsetPrecision = 0;
    private const int DecimalPrecision = 18;
    private const int DecimalScale = 2;

    public static ModelBuilder ApplyRutaVivaConventions(this ModelBuilder modelBuilder)
    {
        List<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes().ToList();
        foreach (IMutableEntityType entityType in entityTypes)
        {
            ApplyRowVersion(modelBuilder, entityType);
            ApplyPropertyConventions(entityType);
        }

        return modelBuilder;
    }

    private static void ApplyRowVersion(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (entityType.BaseType is not null || entityType.IsOwned())
        {
            return;
        }

        if (!typeof(IHasRowVersion).IsAssignableFrom(entityType.ClrType))
        {
            return;
        }

        modelBuilder.Entity(entityType.ClrType).Property(nameof(IHasRowVersion.RowVersion)).IsRowVersion();
    }

    private static void ApplyPropertyConventions(IMutableEntityType entityType)
    {
        List<IMutableProperty> properties = entityType.GetProperties().ToList();
        foreach (IMutableProperty property in properties)
        {
            ApplyPropertyConvention(property);
        }
    }

    private static void ApplyPropertyConvention(IMutableProperty property)
    {
        Type clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (clrType.IsEnum)
        {
            ApplyEnum(property);
            return;
        }

        if (clrType == typeof(DateTimeOffset))
        {
            ApplyDateTimeOffset(property);
            return;
        }

        if (clrType == typeof(decimal))
        {
            ApplyDecimal(property);
        }
    }

    private static void ApplyEnum(IMutableProperty property)
    {
        if (property.GetValueConverter() is not null || property.GetProviderClrType() is not null)
        {
            return;
        }

        property.SetProviderClrType(typeof(string));
        property.SetMaxLength(EnumMaxLength);
        property.SetIsUnicode(false);
    }

    private static void ApplyDateTimeOffset(IMutableProperty property)
    {
        if (property.GetPrecision() is not null)
        {
            return;
        }

        property.SetPrecision(DateTimeOffsetPrecision);
    }

    private static void ApplyDecimal(IMutableProperty property)
    {
        if (property.GetPrecision() is not null)
        {
            return;
        }

        property.SetPrecision(DecimalPrecision);
        property.SetScale(DecimalScale);
    }
}
