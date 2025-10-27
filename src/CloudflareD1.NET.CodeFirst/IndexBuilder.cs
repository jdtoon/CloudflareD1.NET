using System;
using System.Linq.Expressions;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Provides a fluent API for configuring an index
/// </summary>
public class IndexBuilder<TEntity> where TEntity : class
{
    private readonly ModelBuilder _modelBuilder;
    private readonly string[] _propertyNames;
    private bool _isUnique;
    private string? _name;

    internal IndexBuilder(ModelBuilder modelBuilder, string[] propertyNames)
    {
        _modelBuilder = modelBuilder;
        _propertyNames = propertyNames;
    }

    /// <summary>
    /// Configures the index as unique
    /// </summary>
    public IndexBuilder<TEntity> IsUnique()
    {
        _isUnique = true;
        _modelBuilder.SetIndexUnique(typeof(TEntity), _propertyNames);
        return this;
    }

    /// <summary>
    /// Configures the index name
    /// </summary>
    public IndexBuilder<TEntity> HasName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Index name cannot be null or whitespace", nameof(name));

        _name = name;
        _modelBuilder.SetIndexName(typeof(TEntity), _propertyNames, name);
        return this;
    }

    internal bool GetIsUnique() => _isUnique;
    internal string? GetName() => _name;
    internal string[] GetPropertyNames() => _propertyNames;
}
