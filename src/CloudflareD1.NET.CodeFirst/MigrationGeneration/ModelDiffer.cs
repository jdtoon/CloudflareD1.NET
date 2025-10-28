using System;
using System.Threading.Tasks;
using CloudflareD1.NET.CodeFirst.Converters;
using CloudflareD1.NET.CodeFirst.Metadata;
using CloudflareD1.NET.Migrations;

namespace CloudflareD1.NET.CodeFirst.MigrationGeneration;

/// <summary>
/// Compares model metadata with the current database schema to detect changes
/// </summary>
public class ModelDiffer
{
    private readonly D1Client _client;

    /// <summary>
    /// Initializes a new instance of ModelDiffer
    /// </summary>
    /// <param name="client">The D1 client to use for schema introspection</param>
    public ModelDiffer(D1Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Compares the model metadata with the current database schema
    /// </summary>
    /// <param name="modelMetadata">The model metadata from D1Context</param>
    /// <returns>A tuple containing the old database schema and the new model schema</returns>
    public async Task<(DatabaseSchema CurrentSchema, DatabaseSchema ModelSchema)> CompareAsync(ModelMetadata modelMetadata)
    {
        if (modelMetadata == null)
            throw new ArgumentNullException(nameof(modelMetadata));

        // Get current database schema using SchemaIntrospector
        var introspector = new SchemaIntrospector(_client);
        var currentSchema = await introspector.GetSchemaAsync();

        // Convert model metadata to database schema
        var modelSchema = ModelSchemaConverter.ToDatabaseSchema(modelMetadata);

        return (currentSchema, modelSchema);
    }

    /// <summary>
    /// Determines if there are any differences between the model and database
    /// </summary>
    /// <param name="modelMetadata">The model metadata from D1Context</param>
    /// <returns>True if there are differences, false otherwise</returns>
    public async Task<bool> HasChangesAsync(ModelMetadata modelMetadata)
    {
        var (currentSchema, modelSchema) = await CompareAsync(modelMetadata);
        return HasDifferences(currentSchema, modelSchema);
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
