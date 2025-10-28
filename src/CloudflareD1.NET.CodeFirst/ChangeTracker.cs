using System;
using System.Collections.Generic;
using System.Linq;
using CloudflareD1.NET.CodeFirst.Metadata;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Tracks changes to entities for SaveChanges
/// </summary>
public class ChangeTracker
{
    private readonly List<ITrackedEntry> _entries = new();

    /// <summary>
    /// All tracked entries
    /// </summary>
    public IEnumerable<ITrackedEntry> Entries => _entries;

    internal EntityEntry<TEntity> TrackAdd<TEntity>(TEntity entity, EntityTypeMetadata metadata) where TEntity : class
    {
        var entry = new EntityEntry<TEntity>(entity, metadata, EntityState.Added);
        _entries.Add(entry);
        return entry;
    }

    internal EntityEntry<TEntity> TrackUpdate<TEntity>(TEntity entity, EntityTypeMetadata metadata) where TEntity : class
    {
        var entry = FindExistingEntry(entity) as EntityEntry<TEntity>;
        if (entry == null)
        {
            entry = new EntityEntry<TEntity>(entity, metadata, EntityState.Modified);
            // Capture original values snapshot at time of tracking
            foreach (var prop in metadata.Properties)
            {
                entry.OriginalValues[prop] = prop.PropertyInfo.GetValue(entity);
            }
            _entries.Add(entry);
        }
        else
        {
            ((ITrackedEntry)entry).SetState(EntityState.Modified);
        }
        return entry;
    }

    internal EntityEntry<TEntity> TrackRemove<TEntity>(TEntity entity, EntityTypeMetadata metadata) where TEntity : class
    {
        var entry = FindExistingEntry(entity) as EntityEntry<TEntity>;
        if (entry == null)
        {
            entry = new EntityEntry<TEntity>(entity, metadata, EntityState.Deleted);
            _entries.Add(entry);
        }
        else
        {
            ((ITrackedEntry)entry).SetState(EntityState.Deleted);
        }
        return entry;
    }

    private ITrackedEntry? FindExistingEntry(object entity)
    {
        return _entries.FirstOrDefault(e => ReferenceEquals(e.EntityObject, entity));
    }

    /// <summary>
    /// Clears tracking for all entries or accepts changes based on their state
    /// </summary>
    public void AcceptAllChanges()
    {
        // Remove Deleted entries; mark Added/Modified entries as Unchanged
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var e = _entries[i];
            if (e.State == EntityState.Deleted)
            {
                _entries.RemoveAt(i);
            }
            else if (e.State == EntityState.Added || e.State == EntityState.Modified)
            {
                e.SetState(EntityState.Unchanged);
                e.OriginalValues.Clear();
            }
        }
    }

    /// <summary>
    /// Detach all entries from tracking
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}
