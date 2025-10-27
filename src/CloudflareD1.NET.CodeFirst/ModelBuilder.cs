using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.Metadata;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Provides a fluent API for configuring the model
/// </summary>
public class ModelBuilder
{
    private readonly Dictionary<Type, EntityTypeBuilder> _entityBuilders = new();

    /// <summary>
    /// Configures an entity type
    /// </summary>
    public EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        if (!_entityBuilders.TryGetValue(type, out var builder))
        {
            builder = new EntityTypeBuilder<TEntity>(this);
            _entityBuilders[type] = builder;
        }
        return (EntityTypeBuilder<TEntity>)builder;
    }

    /// <summary>
    /// Builds the model metadata
    /// </summary>
    internal ModelMetadata Build(Type contextType)
    {
        var model = new ModelMetadata();

        // Discover entity types from D1Set properties
        var setProperties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(D1Set<>));

        foreach (var prop in setProperties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];

            // Get or create builder for this entity
            if (!_entityBuilders.ContainsKey(entityType))
            {
                var entityMethod = typeof(ModelBuilder).GetMethod(nameof(Entity))!
                    .MakeGenericMethod(entityType);
                entityMethod.Invoke(this, null);
            }
        }

        // Build metadata for each entity
        foreach (var kvp in _entityBuilders)
        {
            var entityMetadata = BuildEntityMetadata(kvp.Key, kvp.Value);
            model.AddEntity(kvp.Key, entityMetadata);
        }

        return model;
    }

    private EntityTypeMetadata BuildEntityMetadata(Type entityType, EntityTypeBuilder builder)
    {
        var metadata = new EntityTypeMetadata
        {
            ClrType = entityType,
            TableName = builder.GetTableName() ?? GetDefaultTableName(entityType)
        };

        // Discover properties
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !p.GetCustomAttributes<NotMappedAttribute>().Any());

        foreach (var prop in properties)
        {
            var propMetadata = BuildPropertyMetadata(prop, builder);
            metadata.Properties.Add(propMetadata);

            if (propMetadata.IsPrimaryKey)
            {
                metadata.PrimaryKey.Add(propMetadata);
            }
        }

        // If no primary key was explicitly configured, look for Id or [EntityName]Id
        if (metadata.PrimaryKey.Count == 0)
        {
            var idProp = metadata.Properties.FirstOrDefault(p =>
                p.PropertyInfo.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.PropertyInfo.Name.Equals($"{entityType.Name}Id", StringComparison.OrdinalIgnoreCase));

            if (idProp != null)
            {
                idProp.IsPrimaryKey = true;
                idProp.IsAutoIncrement = IsIntegerType(idProp.PropertyInfo.PropertyType);
                metadata.PrimaryKey.Add(idProp);
            }
        }

        // Build foreign keys
        BuildForeignKeys(metadata, builder);

        return metadata;
    }

    private PropertyMetadata BuildPropertyMetadata(PropertyInfo prop, EntityTypeBuilder builder)
    {
        var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
        var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
        var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();

        var metadata = new PropertyMetadata
        {
            PropertyInfo = prop,
            ColumnName = columnAttr?.Name ?? ToSnakeCase(prop.Name),
            ColumnType = columnAttr?.TypeName ?? GetDefaultColumnType(prop.PropertyType),
            IsRequired = requiredAttr != null || !IsNullableType(prop.PropertyType),
            IsPrimaryKey = keyAttr != null
        };

        // Auto-increment for integer primary keys
        if (metadata.IsPrimaryKey && IsIntegerType(prop.PropertyType))
        {
            metadata.IsAutoIncrement = true;
        }

        return metadata;
    }

    private void BuildForeignKeys(EntityTypeMetadata metadata, EntityTypeBuilder builder)
    {
        foreach (var prop in metadata.ClrType.GetProperties())
        {
            var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
            if (fkAttr != null)
            {
                // Find the navigation property referenced by the FK attribute
                var navProp = metadata.ClrType.GetProperty(fkAttr.Name);
                if (navProp != null && navProp.PropertyType != typeof(string))
                {
                    var fkMetadata = new ForeignKeyMetadata
                    {
                        PrincipalType = navProp.PropertyType,
                        DependentType = metadata.ClrType
                    };

                    // Add the foreign key property
                    var fkPropMetadata = metadata.Properties.FirstOrDefault(p => p.PropertyInfo == prop);
                    if (fkPropMetadata != null)
                    {
                        fkMetadata.DependentProperties.Add(fkPropMetadata);
                    }

                    metadata.ForeignKeys.Add(fkMetadata);
                }
            }
        }
    }

    private string GetDefaultTableName(Type entityType)
    {
        var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            return tableAttr.Name;
        }

        // Default: pluralize and convert to snake_case
        var name = entityType.Name;
        if (!name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            name += "s";
        }
        return ToSnakeCase(name);
    }

    private string GetDefaultColumnType(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(short) || underlyingType == typeof(byte) ||
            underlyingType == typeof(bool))
        {
            return "INTEGER";
        }

        if (underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
        {
            return "REAL";
        }

        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            return "TEXT"; // ISO 8601 format
        }

        if (underlyingType == typeof(Guid))
        {
            return "TEXT";
        }

        if (underlyingType == typeof(byte[]))
        {
            return "BLOB";
        }

        return "TEXT";
    }

    private bool IsNullableType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private bool IsIntegerType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(int) || underlyingType == typeof(long) ||
               underlyingType == typeof(short) || underlyingType == typeof(byte);
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
