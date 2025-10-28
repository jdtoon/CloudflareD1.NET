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
    // Stores fluent relationship configurations collected via HasOne/HasMany
    private readonly List<RelationshipConfiguration> _relationships = new();
    // Stores fluent index configurations collected via HasIndex
    private readonly List<IndexConfiguration> _indexes = new();
    // Stores fluent key configurations collected via HasKey
    private readonly Dictionary<Type, List<string>> _keys = new();

    internal class RelationshipConfiguration
    {
        public Type PrincipalType { get; set; } = null!; // referenced
        public Type DependentType { get; set; } = null!; // has FK
        public string? PrincipalNavigation { get; set; }
        public string? DependentNavigation { get; set; }
        public string? ForeignKeyProperty { get; set; }
        public string? PrincipalKeyProperty { get; set; }
        public bool IsRequired { get; set; }
        public DeleteBehavior DeleteBehavior { get; set; } = DeleteBehavior.NoAction;
        public bool IsOneToOne { get; set; }
    }

    internal class IndexConfiguration
    {
        public Type EntityType { get; set; } = null!;
        public string[] PropertyNames { get; set; } = Array.Empty<string>();
        public bool IsUnique { get; set; }
        public string? Name { get; set; }
    }

    // Called by relationship builders
    internal void AddRelationship(Type principalType, Type dependentType, string? principalNavigation, string? dependentNavigation, bool isRequired, bool isOneToOne)
    {
        _relationships.Add(new RelationshipConfiguration
        {
            PrincipalType = principalType,
            DependentType = dependentType,
            PrincipalNavigation = principalNavigation,
            DependentNavigation = dependentNavigation,
            IsRequired = isRequired,
            IsOneToOne = isOneToOne
        });
    }

    internal void SetForeignKeyProperty(Type dependentType, Type principalType, string foreignKeyProperty)
    {
        var rel = _relationships.LastOrDefault(r => r.DependentType == dependentType && r.PrincipalType == principalType);
        if (rel != null) rel.ForeignKeyProperty = foreignKeyProperty;
    }

    internal void SetPrincipalKeyProperty(Type dependentType, Type principalType, string principalKeyProperty)
    {
        var rel = _relationships.LastOrDefault(r => r.DependentType == dependentType && r.PrincipalType == principalType);
        if (rel != null) rel.PrincipalKeyProperty = principalKeyProperty;
    }

    internal void SetRelationshipRequired(Type dependentType, Type principalType)
    {
        var rel = _relationships.LastOrDefault(r => r.DependentType == dependentType && r.PrincipalType == principalType);
        if (rel != null) rel.IsRequired = true;
    }

    internal void SetDeleteBehavior(Type dependentType, Type principalType, DeleteBehavior deleteBehavior)
    {
        var rel = _relationships.LastOrDefault(r => r.DependentType == dependentType && r.PrincipalType == principalType);
        if (rel != null) rel.DeleteBehavior = deleteBehavior;
    }

    // Called by entity builder for HasKey
    internal void SetEntityKey(Type entityType, IEnumerable<string> propertyNames)
    {
        _keys[entityType] = propertyNames.ToList();
    }

    // Called by index builders
    internal void AddIndex(Type entityType, string[] propertyNames)
    {
        _indexes.Add(new IndexConfiguration
        {
            EntityType = entityType,
            PropertyNames = propertyNames
        });
    }

    internal void SetIndexUnique(Type entityType, string[] propertyNames)
    {
        var idx = _indexes.LastOrDefault(i => i.EntityType == entityType &&
            i.PropertyNames.SequenceEqual(propertyNames));
        if (idx != null) idx.IsUnique = true;
    }

    internal void SetIndexName(Type entityType, string[] propertyNames, string name)
    {
        var idx = _indexes.LastOrDefault(i => i.EntityType == entityType &&
            i.PropertyNames.SequenceEqual(propertyNames));
        if (idx != null) idx.Name = name;
    }

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
    public ModelMetadata Build(Type contextType)
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

        // Discover properties: include only supported scalar types and exclude navigations/collections unless explicitly marked
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !p.GetCustomAttributes<NotMappedAttribute>().Any())
            .Where(p => IsSupportedColumnType(p.PropertyType));

        foreach (var prop in properties)
        {
            var propMetadata = BuildPropertyMetadata(prop, builder);
            metadata.Properties.Add(propMetadata);

            if (propMetadata.IsPrimaryKey)
            {
                metadata.PrimaryKey.Add(propMetadata);
            }
        }

        // Apply fluent HasKey configuration if present
        if (_keys.TryGetValue(entityType, out var keyProps) && keyProps.Count > 0)
        {
            // Reset any prior PK flags
            foreach (var p in metadata.Properties)
            {
                p.IsPrimaryKey = false;
                p.IsAutoIncrement = false;
            }

            foreach (var keyName in keyProps)
            {
                var pm = metadata.Properties.FirstOrDefault(p => p.PropertyInfo.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                if (pm != null)
                {
                    pm.IsPrimaryKey = true;
                }
            }

            // If single integer key, mark as autoincrement by convention
            if (metadata.Properties.Count(p => p.IsPrimaryKey) == 1)
            {
                var pk = metadata.Properties.First(p => p.IsPrimaryKey);
                if (IsIntegerType(pk.PropertyInfo.PropertyType))
                {
                    pk.IsAutoIncrement = true;
                }
            }

            // Rebuild PrimaryKey list from property flags
            metadata.PrimaryKey.Clear();
            foreach (var p in metadata.Properties.Where(p => p.IsPrimaryKey))
            {
                metadata.PrimaryKey.Add(p);
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

        // Build indexes
        BuildIndexes(metadata, builder);

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
        // 1) Fluent API relationships
        foreach (var rel in _relationships.Where(r => r.DependentType == metadata.ClrType))
        {
            var fk = new ForeignKeyMetadata
            {
                PrincipalType = rel.PrincipalType,
                DependentType = rel.DependentType,
                OnDelete = GetSqlDeleteBehavior(rel.DeleteBehavior)
            };

            // Resolve FK property: explicit or convention {Principal}Id
            PropertyMetadata? fkProp = null;
            if (!string.IsNullOrWhiteSpace(rel.ForeignKeyProperty))
            {
                fkProp = metadata.Properties.FirstOrDefault(p => p.PropertyInfo.Name.Equals(rel.ForeignKeyProperty, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var conventionName = rel.PrincipalType.Name + "Id";
                fkProp = metadata.Properties.FirstOrDefault(p => p.PropertyInfo.Name.Equals(conventionName, StringComparison.OrdinalIgnoreCase));
            }

            if (fkProp != null)
            {
                fk.DependentProperties.Add(fkProp);
                if (rel.IsRequired)
                {
                    fkProp.IsRequired = true;
                }

                // For one-to-one relationships, enforce a unique constraint on the FK column
                if (rel.IsOneToOne)
                {
                    // Add a unique index on the FK column if not already present
                    var exists = metadata.Indexes.Any(ix => ix.IsUnique && ix.Properties.Count == 1 && ix.Properties[0] == fkProp);
                    if (!exists)
                    {
                        metadata.Indexes.Add(new IndexMetadata
                        {
                            IsUnique = true,
                            Name = $"ux_{metadata.TableName}_{fkProp.ColumnName}",
                            Properties = { fkProp }
                        });
                    }
                }
            }

            // Principal key override if specified
            if (!string.IsNullOrWhiteSpace(rel.PrincipalKeyProperty))
            {
                fk.PrincipalKeyPropertyName = rel.PrincipalKeyProperty;
            }

            metadata.ForeignKeys.Add(fk);
        }

        // 2) [ForeignKey] attribute on FK property pointing to navigation
        foreach (var prop in metadata.ClrType.GetProperties())
        {
            var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
            if (fkAttr == null) continue;

            var navProp = metadata.ClrType.GetProperty(fkAttr.Name);
            if (navProp == null || navProp.PropertyType == typeof(string)) continue;

            // If a matching fluent relationship already exists, skip attribute to avoid duplicates
            if (_relationships.Any(r => r.DependentType == metadata.ClrType && r.PrincipalType == navProp.PropertyType))
                continue;

            var fkMetadata = new ForeignKeyMetadata
            {
                PrincipalType = navProp.PropertyType,
                DependentType = metadata.ClrType
            };

            var fkPropMetadata = metadata.Properties.FirstOrDefault(p => p.PropertyInfo == prop);
            if (fkPropMetadata != null)
            {
                fkMetadata.DependentProperties.Add(fkPropMetadata);
            }

            metadata.ForeignKeys.Add(fkMetadata);
        }
    }

    private void BuildIndexes(EntityTypeMetadata metadata, EntityTypeBuilder builder)
    {
        // 1) Fluent API indexes
        foreach (var idx in _indexes.Where(i => i.EntityType == metadata.ClrType))
        {
            var indexMetadata = new IndexMetadata
            {
                IsUnique = idx.IsUnique,
                Name = idx.Name
            };

            // Resolve properties
            foreach (var propName in idx.PropertyNames)
            {
                var prop = metadata.Properties.FirstOrDefault(p =>
                    p.PropertyInfo.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    indexMetadata.Properties.Add(prop);
                }
            }

            // Generate name if not specified
            if (string.IsNullOrWhiteSpace(indexMetadata.Name))
            {
                var columnNames = string.Join("_", indexMetadata.Properties.Select(p => p.ColumnName));
                indexMetadata.Name = $"ix_{metadata.TableName}_{columnNames}";
            }

            metadata.Indexes.Add(indexMetadata);
        }

        // 2) [Index] attributes
        var indexAttrs = metadata.ClrType.GetCustomAttributes<IndexAttribute>();
        foreach (var attr in indexAttrs)
        {
            // Check if this index already exists from fluent config (by property names)
            var propertyNames = attr.PropertyNames;
            var alreadyExists = _indexes.Any(i =>
                i.EntityType == metadata.ClrType &&
                i.PropertyNames.SequenceEqual(propertyNames, StringComparer.OrdinalIgnoreCase));

            if (alreadyExists)
                continue; // Fluent takes precedence

            var indexMetadata = new IndexMetadata
            {
                IsUnique = attr.IsUnique,
                Name = attr.Name
            };

            // Resolve properties
            foreach (var propName in propertyNames)
            {
                var prop = metadata.Properties.FirstOrDefault(p =>
                    p.PropertyInfo.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    indexMetadata.Properties.Add(prop);
                }
            }

            // Generate name if not specified
            if (string.IsNullOrWhiteSpace(indexMetadata.Name))
            {
                var columnNames = string.Join("_", indexMetadata.Properties.Select(p => p.ColumnName));
                indexMetadata.Name = $"ix_{metadata.TableName}_{columnNames}";
            }

            metadata.Indexes.Add(indexMetadata);
        }
    }

    private string? GetSqlDeleteBehavior(DeleteBehavior behavior)
    {
        return behavior switch
        {
            DeleteBehavior.NoAction => "NO ACTION",
            DeleteBehavior.Cascade => "CASCADE",
            DeleteBehavior.SetNull => "SET NULL",
            DeleteBehavior.Restrict => "RESTRICT",
            _ => null
        };
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

        if (underlyingType.IsEnum)
        {
            // Store enums as TEXT by default (name); numeric also works in SQLite, but TEXT is clearer
            return "TEXT";
        }

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

    /// <summary>
    /// Determines if a CLR type is supported as a simple column mapping.
    /// Excludes navigation properties (reference types other than string/byte[]) and collections.
    /// </summary>
    private bool IsSupportedColumnType(Type type)
    {
        // Unwrap Nullable<T>
        var t = Nullable.GetUnderlyingType(type) ?? type;

        // Allow byte[] explicitly
        if (t == typeof(byte[])) return true;

        // Exclude collections (IEnumerable) except string and byte[]
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string) && t != typeof(byte[]))
        {
            return false;
        }

        // Exclude non-string class types (likely navigations/value objects)
        if (t.IsClass && t != typeof(string))
        {
            return false;
        }

        // Allow enums and primitive/value types commonly stored
        if (t.IsEnum) return true;

        if (t.IsPrimitive) return true; // includes bool, integral, floating point

        if (t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid) || t == typeof(string))
        {
            return true;
        }

        // Fallback: not supported
        return false;
    }
}
