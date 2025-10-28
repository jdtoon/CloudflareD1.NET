using System;
using System.Collections.Generic;
using CloudflareD1.NET.CodeFirst.Metadata;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Represents a tracked entity and its state
/// </summary>
public interface ITrackedEntry
{
    Type EntityType { get; }
    object EntityObject { get; }
    EntityState State { get; }
    EntityTypeMetadata Metadata { get; }
    Dictionary<PropertyMetadata, object?> OriginalValues { get; }
    void SetState(EntityState state);
}

/// <summary>
/// Represents a tracked entity entry
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public class EntityEntry<TEntity> : ITrackedEntry where TEntity : class
{
    internal EntityEntry(TEntity entity, EntityTypeMetadata metadata, EntityState state)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        State = state;
        OriginalValues = new Dictionary<PropertyMetadata, object?>();
    }

    /// <summary>
    /// The tracked entity instance
    /// </summary>
    public TEntity Entity { get; }

    /// <summary>
    /// Current tracking state
    /// </summary>
    public EntityState State { get; private set; }

    /// <summary>
    /// Entity metadata
    /// </summary>
    public EntityTypeMetadata Metadata { get; }

    /// <summary>
    /// Snapshot of original values (optional)
    /// </summary>
    public Dictionary<PropertyMetadata, object?> OriginalValues { get; }

    Type ITrackedEntry.EntityType => typeof(TEntity);
    object ITrackedEntry.EntityObject => Entity!;
    EntityState ITrackedEntry.State => State;
    EntityTypeMetadata ITrackedEntry.Metadata => Metadata;
    Dictionary<PropertyMetadata, object?> ITrackedEntry.OriginalValues => OriginalValues;
    void ITrackedEntry.SetState(EntityState state) => State = state;
}
