using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace RutaViva.Customers.Infrastructure.Persistence;

public static class EnumCheckConstraintExtensions
{
    private const string ConstraintNameFormat = "CK_{0}_{1}";
    private const string ConstraintSqlFormat = "[{0}] IN ({1})";
    private const string LiteralFormat = "'{0}'";
    private const string LiteralSeparator = ", ";

    public static ModelBuilder ApplyEnumCheckConstraints(this ModelBuilder modelBuilder)
    {
        List<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes().Where(IsTableRoot).ToList();
        foreach (IMutableEntityType entityType in entityTypes)
        {
            AddConstraints(entityType);
        }

        return modelBuilder;
    }

    private static bool IsTableRoot(IMutableEntityType entityType) =>
        !entityType.IsOwned() && entityType.GetTableName() is not null;

    private static Type EnumTypeOf(IMutableProperty property) => Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

    private static bool IsEnum(IMutableProperty property) => EnumTypeOf(property).IsEnum;

    private static void AddConstraints(IMutableEntityType entityType)
    {
        List<IMutableProperty> enumProperties = entityType.GetProperties().Where(IsEnum).ToList();
        foreach (IMutableProperty property in enumProperties)
        {
            AddConstraint(entityType, property);
        }
    }

    private static void AddConstraint(IMutableEntityType entityType, IMutableProperty property)
    {
        string column = property.GetColumnName();
        string name = string.Format(CultureInfo.InvariantCulture, ConstraintNameFormat, entityType.GetTableName(), column);
        string sql = string.Format(CultureInfo.InvariantCulture, ConstraintSqlFormat, column, LiteralsOf(EnumTypeOf(property)));
        entityType.AddCheckConstraint(name, sql);
    }

    private static string LiteralsOf(Type enumType) =>
        string.Join(LiteralSeparator, Enum.GetNames(enumType).Select(FormatLiteral));

    private static string FormatLiteral(string name) => string.Format(CultureInfo.InvariantCulture, LiteralFormat, name);
}
