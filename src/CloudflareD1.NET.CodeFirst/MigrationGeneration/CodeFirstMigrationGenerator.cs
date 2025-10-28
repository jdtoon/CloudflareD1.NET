using System;
using System.IO;
using System.Threading.Tasks;
using CloudflareD1.NET.CodeFirst.Metadata;
using CloudflareD1.NET.Migrations;

namespace CloudflareD1.NET.CodeFirst.MigrationGeneration;

/// <summary>
/// Generates migration files from Code-First model changes
/// </summary>
public class CodeFirstMigrationGenerator
{
    private readonly ModelDiffer _differ;
    private readonly MigrationScaffolder _scaffolder;

    /// <summary>
    /// Initializes a new instance of CodeFirstMigrationGenerator
    /// </summary>
    /// <param name="snapshotDirectory">Directory for snapshot files (optional, defaults to current directory)</param>
    public CodeFirstMigrationGenerator(string? snapshotDirectory = null)
    {
        _differ = new ModelDiffer(snapshotDirectory);
        _scaffolder = new MigrationScaffolder();
    }

    /// <summary>
    /// Generates a migration file from model changes
    /// </summary>
    /// <param name="context">The D1Context containing the model</param>
    /// <param name="migrationName">The name of the migration</param>
    /// <param name="outputDirectory">The directory where the migration file will be created</param>
    /// <returns>The path to the generated migration file</returns>
    public async Task<string> GenerateMigrationAsync(D1Context context, string migrationName, string outputDirectory)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (string.IsNullOrWhiteSpace(migrationName))
            throw new ArgumentException("Migration name cannot be empty", nameof(migrationName));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be empty", nameof(outputDirectory));

        // Get model metadata from context
        var modelMetadata = context.GetModelMetadata();

        // Compare with last migration snapshot
        var (lastSnapshot, modelSchema) = await _differ.CompareAsync(modelMetadata);

        // Generate migration code
        var migrationCode = _scaffolder.GenerateMigration(lastSnapshot, modelSchema, migrationName);

        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        // Generate filename with timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{timestamp}_{migrationName}.cs";
        var filePath = Path.Combine(outputDirectory, fileName);

        // Write migration file
        await File.WriteAllTextAsync(filePath, migrationCode);

        // Save the new snapshot
        await SchemaSnapshot.SaveAsync(modelSchema, outputDirectory);

        return filePath;
    }

    /// <summary>
    /// Checks if there are pending model changes that need a migration
    /// </summary>
    /// <param name="context">The D1Context containing the model</param>
    /// <returns>True if there are pending changes, false otherwise</returns>
    public async Task<bool> HasPendingChangesAsync(D1Context context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var modelMetadata = context.GetModelMetadata();
        return await _differ.HasChangesAsync(modelMetadata);
    }

    /// <summary>
    /// Gets a summary of changes between the model and database
    /// </summary>
    /// <param name="context">The D1Context containing the model</param>
    /// <returns>A human-readable summary of changes</returns>
    public async Task<string> GetChangesSummaryAsync(D1Context context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var modelMetadata = context.GetModelMetadata();
        var (currentSchema, modelSchema) = await _differ.CompareAsync(modelMetadata);

        return GenerateChangesSummary(currentSchema, modelSchema);
    }

    private string GenerateChangesSummary(DatabaseSchema current, DatabaseSchema model)
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("Pending model changes:");
        summary.AppendLine();

        // New tables
        foreach (var modelTable in model.Tables)
        {
            if (!current.Tables.Exists(t => t.Name == modelTable.Name))
            {
                summary.AppendLine($"  + Create table '{modelTable.Name}' with {modelTable.Columns.Count} column(s)");
            }
        }

        // Removed tables
        foreach (var currentTable in current.Tables)
        {
            if (!model.Tables.Exists(t => t.Name == currentTable.Name))
            {
                summary.AppendLine($"  - Drop table '{currentTable.Name}'");
            }
        }

        // Modified tables
        foreach (var modelTable in model.Tables)
        {
            var currentTable = current.Tables.Find(t => t.Name == modelTable.Name);
            if (currentTable == null)
                continue;

            // New columns
            foreach (var modelColumn in modelTable.Columns)
            {
                if (!currentTable.Columns.Exists(c => c.Name == modelColumn.Name))
                {
                    summary.AppendLine($"  + Add column '{modelTable.Name}.{modelColumn.Name}' ({modelColumn.Type})");
                }
            }

            // Removed columns
            foreach (var currentColumn in currentTable.Columns)
            {
                if (!modelTable.Columns.Exists(c => c.Name == currentColumn.Name))
                {
                    summary.AppendLine($"  - Drop column '{modelTable.Name}.{currentColumn.Name}'");
                }
            }

            // Modified columns
            foreach (var modelColumn in modelTable.Columns)
            {
                var currentColumn = currentTable.Columns.Find(c => c.Name == modelColumn.Name);
                if (currentColumn != null)
                {
                    if (currentColumn.Type != modelColumn.Type)
                    {
                        summary.AppendLine($"  * Alter column '{modelTable.Name}.{modelColumn.Name}' type: {currentColumn.Type} -> {modelColumn.Type}");
                    }
                    if (currentColumn.NotNull != modelColumn.NotNull)
                    {
                        summary.AppendLine($"  * Alter column '{modelTable.Name}.{modelColumn.Name}' nullability: {!currentColumn.NotNull} -> {!modelColumn.NotNull}");
                    }
                }
            }

            // Indexes
            foreach (var modelIndex in modelTable.Indexes)
            {
                if (!currentTable.Indexes.Exists(i => i.Name == modelIndex.Name))
                {
                    summary.AppendLine($"  + Create index '{modelIndex.Name}' on '{modelTable.Name}'");
                }
            }

            foreach (var currentIndex in currentTable.Indexes)
            {
                if (!modelTable.Indexes.Exists(i => i.Name == currentIndex.Name))
                {
                    summary.AppendLine($"  - Drop index '{currentIndex.Name}' on '{modelTable.Name}'");
                }
            }

            // Foreign keys
            foreach (var modelFk in modelTable.ForeignKeys)
            {
                var fkExists = currentTable.ForeignKeys.Exists(fk =>
                    fk.Column == modelFk.Column &&
                    fk.ReferencedTable == modelFk.ReferencedTable &&
                    fk.ReferencedColumn == modelFk.ReferencedColumn);

                if (!fkExists)
                {
                    summary.AppendLine($"  + Add foreign key '{modelTable.Name}.{modelFk.Column}' -> '{modelFk.ReferencedTable}.{modelFk.ReferencedColumn}'");
                }
            }

            foreach (var currentFk in currentTable.ForeignKeys)
            {
                var fkExists = modelTable.ForeignKeys.Exists(fk =>
                    fk.Column == currentFk.Column &&
                    fk.ReferencedTable == currentFk.ReferencedTable &&
                    fk.ReferencedColumn == currentFk.ReferencedColumn);

                if (!fkExists)
                {
                    summary.AppendLine($"  - Drop foreign key '{modelTable.Name}.{currentFk.Column}' -> '{currentFk.ReferencedTable}.{currentFk.ReferencedColumn}'");
                }
            }
        }

        if (summary.Length == "Pending model changes:\n\n".Length)
        {
            summary.AppendLine("  No changes detected.");
        }

        return summary.ToString();
    }
}
