using System;
using System.Linq;
using System.Linq.Expressions;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Base class for entity type builders
/// </summary>
public abstract class EntityTypeBuilder
{
    private string? _tableName;

    internal string? GetTableName() => _tableName;

    internal void SetTableName(string tableName)
    {
        _tableName = tableName;
    }
}

/// <summary>
/// Provides a fluent API for configuring an entity type
/// </summary>
public class EntityTypeBuilder<TEntity> : EntityTypeBuilder where TEntity : class
{
    private readonly ModelBuilder _modelBuilder;

    internal EntityTypeBuilder(ModelBuilder modelBuilder)
    {
        _modelBuilder = modelBuilder;
    }

    /// <summary>
    /// Configures the table name for this entity
    /// </summary>
    public EntityTypeBuilder<TEntity> ToTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

        SetTableName(tableName);
        return this;
    }

    /// <summary>
    /// Configures a property
    /// </summary>
    public PropertyBuilder<TProperty> Property<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));

        return new PropertyBuilder<TProperty>();
    }

    /// <summary>
    /// Configures the primary key
    /// </summary>
    public EntityTypeBuilder<TEntity> HasKey<TKey>(
        Expression<Func<TEntity, TKey>> keyExpression)
    {
        if (keyExpression == null)
            throw new ArgumentNullException(nameof(keyExpression));

        // Key configuration is stored but currently we rely on [Key] attribute
        // This is here for API completeness
        return this;
    }

    /// <summary>
    /// Configures a one-to-many relationship
    /// </summary>
    public ReferenceCollectionBuilder<TEntity, TRelated> HasMany<TRelated>(
        Expression<Func<TEntity, System.Collections.Generic.IEnumerable<TRelated>?>> navigationExpression)
        where TRelated : class
    {
        if (navigationExpression == null)
            throw new ArgumentNullException(nameof(navigationExpression));

        return new ReferenceCollectionBuilder<TEntity, TRelated>(_modelBuilder, navigationExpression);
    }

    /// <summary>
    /// Configures a many-to-one relationship
    /// </summary>
    public ReferenceReferenceBuilder<TRelated, TEntity> HasOne<TRelated>(
        Expression<Func<TEntity, TRelated?>> navigationExpression)
        where TRelated : class
    {
        if (navigationExpression == null)
            throw new ArgumentNullException(nameof(navigationExpression));

        return new ReferenceReferenceBuilder<TRelated, TEntity>(_modelBuilder, navigationExpression);
    }

    /// <summary>
    /// Configures an index on a single property
    /// </summary>
    public IndexBuilder<TEntity> HasIndex<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));

        var propertyName = GetPropertyName(propertyExpression);
        _modelBuilder.AddIndex(typeof(TEntity), new[] { propertyName });
        return new IndexBuilder<TEntity>(_modelBuilder, new[] { propertyName });
    }

    /// <summary>
    /// Configures a composite index on multiple properties
    /// </summary>
    public IndexBuilder<TEntity> HasIndex(params Expression<Func<TEntity, object>>[] propertyExpressions)
    {
        if (propertyExpressions == null || propertyExpressions.Length == 0)
            throw new ArgumentException("At least one property expression is required", nameof(propertyExpressions));

        var propertyNames = propertyExpressions.Select(GetPropertyName).ToArray();
        _modelBuilder.AddIndex(typeof(TEntity), propertyNames);
        return new IndexBuilder<TEntity>(_modelBuilder, propertyNames);
    }

    private static string GetPropertyName<T>(Expression<T> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException($"Expression '{expression}' is not a valid property expression");
    }
}

/// <summary>
/// Provides a fluent API for configuring a property
/// </summary>
public class PropertyBuilder<TProperty>
{
    /// <summary>
    /// Configures the column name
    /// </summary>
    public PropertyBuilder<TProperty> HasColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        // Column name configuration - currently relies on [Column] attribute
        return this;
    }

    /// <summary>
    /// Configures the column type
    /// </summary>
    public PropertyBuilder<TProperty> HasColumnType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

        // Column type configuration - currently relies on [Column] attribute
        return this;
    }

    /// <summary>
    /// Configures the property as required (NOT NULL)
    /// </summary>
    public PropertyBuilder<TProperty> IsRequired()
    {
        // Required configuration - currently relies on [Required] attribute
        return this;
    }

    /// <summary>
    /// Configures the maximum length
    /// </summary>
    public PropertyBuilder<TProperty> HasMaxLength(int maxLength)
    {
        if (maxLength <= 0)
            throw new ArgumentException("Max length must be positive", nameof(maxLength));

        // Max length configuration - for future validation
        return this;
    }
}
