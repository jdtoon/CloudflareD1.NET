using System;
using System.Collections.Generic;

namespace CloudflareD1.NET.CodeFirst.Metadata;

/// <summary>
/// Represents metadata for the entire database model
/// </summary>
public class ModelMetadata
{
    private readonly Dictionary<Type, EntityTypeMetadata> _entities = new();

    /// <summary>
    /// Gets all entity types in the model
    /// </summary>
    public IReadOnlyDictionary<Type, EntityTypeMetadata> Entities => _entities;

    /// <summary>
    /// Adds an entity type to the model
    /// </summary>
    internal void AddEntity(Type type, EntityTypeMetadata metadata)
    {
        _entities[type] = metadata;
    }

    /// <summary>
    /// Gets metadata for an entity type
    /// </summary>
    public EntityTypeMetadata? GetEntity(Type type)
    {
        return _entities.TryGetValue(type, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Gets metadata for an entity type
    /// </summary>
    public EntityTypeMetadata? GetEntity<TEntity>() where TEntity : class
    {
        return GetEntity(typeof(TEntity));
    }
}
