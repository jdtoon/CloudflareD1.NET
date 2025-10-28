using System;
using System.Threading.Tasks;
using CloudflareD1.NET.CodeFirst.Converters;
using CloudflareD1.NET.CodeFirst.Metadata;
using CloudflareD1.NET.Migrations;

namespace CloudflareD1.NET.CodeFirst.MigrationGeneration;

/// <summary>
/// Compares model metadata with the last migration snapshot to detect changes
/// </summary>
public class ModelDiffer
{
    private readonly string? _snapshotDirectory;

    /// <summary>
    /// Initializes a new instance of ModelDiffer
    /// </summary>
    /// <param name="snapshotDirectory">Directory containing the snapshot file (optional, defaults to current directory)</param>
    public ModelDiffer(string? snapshotDirectory = null)
    {
        _snapshotDirectory = snapshotDirectory;
    }

    /// <summary>
    /// Compares the model metadata with the last migration snapshot
    /// </summary>
    /// <param name="modelMetadata">The model metadata from D1Context</param>
    /// <returns>A tuple containing the last snapshot schema and the new model schema</returns>
    public async Task<(DatabaseSchema LastSnapshot, DatabaseSchema ModelSchema)> CompareAsync(ModelMetadata modelMetadata)
    {
        if (modelMetadata == null)
            throw new ArgumentNullException(nameof(modelMetadata));

        // Load the last snapshot (if it exists)
        var lastSnapshot = await SchemaSnapshot.LoadAsync(_snapshotDirectory) ?? new DatabaseSchema { Tables = new() };

        // Convert model metadata to database schema
        var modelSchema = ModelSchemaConverter.ToDatabaseSchema(modelMetadata);

        return (lastSnapshot, modelSchema);
    }

    /// <summary>
    /// Determines if there are any differences between the model and last snapshot
    /// </summary>
    /// <param name="modelMetadata">The model metadata from D1Context</param>
    /// <returns>True if there are differences, false otherwise</returns>
    public async Task<bool> HasChangesAsync(ModelMetadata modelMetadata)
    {
        var (lastSnapshot, modelSchema) = await CompareAsync(modelMetadata);
        return HasDifferences(lastSnapshot, modelSchema);
    }

    private bool HasDifferences(DatabaseSchema current, DatabaseSchema model)
    {
        // Check for new or removed tables
        if (current.Tables.Count != model.Tables.Count)
            return true;

        foreach (var modelTable in model.Tables)
        {
            var currentTable = current.Tables.Find(t => t.Name == modelTable.Name);

            // New table
            if (currentTable == null)
                return true;

            // Check columns
            if (currentTable.Columns.Count != modelTable.Columns.Count)
                return true;

            foreach (var modelColumn in modelTable.Columns)
            {
                var currentColumn = currentTable.Columns.Find(c => c.Name == modelColumn.Name);

                // New column
                if (currentColumn == null)
                    return true;

                // Column property changes
                if (currentColumn.Type != modelColumn.Type ||
                    currentColumn.NotNull != modelColumn.NotNull ||
                    currentColumn.IsPrimaryKey != modelColumn.IsPrimaryKey)
                    return true;
            }

            // Check for removed columns
            foreach (var currentColumn in currentTable.Columns)
            {
                if (!modelTable.Columns.Exists(c => c.Name == currentColumn.Name))
                    return true;
            }

            // Check indexes
            if (currentTable.Indexes.Count != modelTable.Indexes.Count)
                return true;

            // Check foreign keys
            if (currentTable.ForeignKeys.Count != modelTable.ForeignKeys.Count)
                return true;
        }

        // Check for removed tables
        foreach (var currentTable in current.Tables)
        {
            if (!model.Tables.Exists(t => t.Name == currentTable.Name))
                return true;
        }

        return false;
    }
}
