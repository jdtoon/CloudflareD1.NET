using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Gets the list of properties that have been modified since tracking began.
    /// Compares current property values with OriginalValues snapshot.
    /// </summary>
    /// <returns>List of modified properties</returns>
    List<PropertyMetadata> GetModifiedProperties();
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

    /// <summary>
    /// Gets the list of properties that have been modified since tracking began.
    /// Compares current property values with OriginalValues snapshot.
    /// </summary>
    /// <returns>List of modified properties</returns>
    public List<PropertyMetadata> GetModifiedProperties()
    {
        var modifiedProps = new List<PropertyMetadata>();

        // If no original values captured (e.g., Added state), return empty list
        if (OriginalValues.Count == 0)
        {
            return modifiedProps;
        }

        foreach (var prop in Metadata.Properties)
        {
            // Skip primary keys (they shouldn't change)
            if (prop.IsPrimaryKey)
            {
                continue;
            }

            // Get current and original values
            var currentValue = prop.PropertyInfo.GetValue(Entity);
            if (OriginalValues.TryGetValue(prop, out var originalValue))
            {
                // Compare values - handle nulls properly
                if (!ValuesEqual(currentValue, originalValue))
                {
                    modifiedProps.Add(prop);
                }
            }
        }

        return modifiedProps;
    }

    private static bool ValuesEqual(object? value1, object? value2)
    {
        // Both null
        if (value1 == null && value2 == null)
        {
            return true;
        }

        // One null, one not
        if (value1 == null || value2 == null)
        {
            return false;
        }

        // Use Equals for value comparison
        return value1.Equals(value2);
    }
}
