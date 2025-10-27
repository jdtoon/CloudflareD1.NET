using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudflareD1.NET.Migrations;

/// <summary>
/// Compares two database schemas and generates migration code
/// </summary>
public class MigrationScaffolder
{
    /// <summary>
    /// Generates a migration by comparing old and new schemas
    /// </summary>
    public string GenerateMigration(DatabaseSchema? oldSchema, DatabaseSchema newSchema, string migrationName)
    {
        var upOperations = new List<string>();
        var downOperations = new List<string>();

        oldSchema ??= new DatabaseSchema(); // Treat null as empty schema

        // Find new tables
        foreach (var newTable in newSchema.Tables)
        {
            var oldTable = oldSchema.Tables.FirstOrDefault(t => t.Name == newTable.Name);

            if (oldTable == null)
            {
                // Table is new - create it
                upOperations.Add(GenerateCreateTable(newTable));
                downOperations.Add($"builder.DropTable(\"{newTable.Name}\");");

                // Create indexes
                foreach (var index in newTable.Indexes)
                {
                    upOperations.Add(GenerateCreateIndex(newTable.Name, index));
                }
            }
            else
            {
                // Table exists - check for column changes
                var tableChanges = CompareColumns(oldTable, newTable);
                upOperations.AddRange(tableChanges.UpOperations);
                downOperations.AddRange(tableChanges.DownOperations);

                // Check for index changes
                var indexChanges = CompareIndexes(oldTable, newTable);
                upOperations.AddRange(indexChanges.UpOperations);
                downOperations.AddRange(indexChanges.DownOperations);
            }
        }

        // Find dropped tables
        foreach (var oldTable in oldSchema.Tables)
        {
            if (!newSchema.Tables.Any(t => t.Name == oldTable.Name))
            {
                upOperations.Add($"builder.DropTable(\"{oldTable.Name}\");");
                downOperations.Add(GenerateCreateTable(oldTable));
            }
        }

        // Generate migration file
        return GenerateMigrationFile(migrationName, upOperations, downOperations);
    }

    private string GenerateCreateTable(TableSchema table)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"builder.CreateTable(\"{table.Name}\", t =>");
        sb.AppendLine("        {");

        foreach (var column in table.Columns)
        {
            sb.Append($"            t.{MapType(column.Type)}(\"{column.Name}\")");

            if (column.IsPrimaryKey)
                sb.Append(".PrimaryKey()");

            if (column.NotNull && !column.IsPrimaryKey)
                sb.Append(".NotNull()");

            if (!string.IsNullOrEmpty(column.DefaultValue))
                sb.Append($".Default({FormatDefaultValue(column.DefaultValue)})");

            sb.AppendLine(";");
        }

        // Add foreign keys as table constraints
        foreach (var fk in table.ForeignKeys)
        {
            if (!string.IsNullOrEmpty(fk.OnDelete))
            {
                sb.AppendLine($"            t.ForeignKey(\"{fk.Column}\", \"{fk.ReferencedTable}\", \"{fk.ReferencedColumn}\", \"{fk.OnDelete}\");");
            }
            else
            {
                sb.AppendLine($"            t.ForeignKey(\"{fk.Column}\", \"{fk.ReferencedTable}\", \"{fk.ReferencedColumn}\");");
            }
        }

        sb.Append("        });");
        return sb.ToString();
    }

    private string GenerateCreateIndex(string tableName, IndexSchema index)
    {
        // Parse index SQL to determine if unique and which columns
        var isUnique = index.Sql?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ?? false;

        // Extract column name from SQL (simplified - assumes single column index)
        var columnName = ExtractIndexColumn(index.Sql);

        if (isUnique)
            return $"builder.CreateUniqueIndex(\"{tableName}\", \"{index.Name}\", \"{columnName}\");";
        else
            return $"builder.CreateIndex(\"{tableName}\", \"{index.Name}\", \"{columnName}\");";
    }

    private (List<string> UpOperations, List<string> DownOperations) CompareColumns(
        TableSchema oldTable, TableSchema newTable)
    {
        var upOps = new List<string>();
        var downOps = new List<string>();

        // Find new columns
        foreach (var newCol in newTable.Columns)
        {
            if (!oldTable.Columns.Any(c => c.Name == newCol.Name))
            {
                var colDef = new StringBuilder();
                colDef.Append($"builder.AlterTable(\"{newTable.Name}\", t =>");
                colDef.AppendLine();
                colDef.AppendLine("        {");
                colDef.Append($"            t.{MapType(newCol.Type)}(\"{newCol.Name}\")");

                if (newCol.NotNull)
                    colDef.Append(".NotNull()");

                if (!string.IsNullOrEmpty(newCol.DefaultValue))
                    colDef.Append($".Default({FormatDefaultValue(newCol.DefaultValue)})");

                colDef.AppendLine(";");
                colDef.Append("        });");

                upOps.Add(colDef.ToString());

                // SQLite doesn't support DROP COLUMN easily
                downOps.Add($"// Note: SQLite doesn't support DROP COLUMN. " +
                           $"To remove '{newCol.Name}' from '{newTable.Name}', recreate the table.");
            }
        }

        // Note: We don't detect removed columns automatically since SQLite can't drop them
        // Users would need to manually handle table recreation

        return (upOps, downOps);
    }

    private (List<string> UpOperations, List<string> DownOperations) CompareIndexes(
        TableSchema oldTable, TableSchema newTable)
    {
        var upOps = new List<string>();
        var downOps = new List<string>();

        // Find new indexes
        foreach (var newIdx in newTable.Indexes)
        {
            if (!oldTable.Indexes.Any(i => i.Name == newIdx.Name))
            {
                upOps.Add(GenerateCreateIndex(newTable.Name, newIdx));
                downOps.Add($"builder.DropIndex(\"{newTable.Name}\", \"{newIdx.Name}\");");
            }
        }

        // Find dropped indexes
        foreach (var oldIdx in oldTable.Indexes)
        {
            if (!newTable.Indexes.Any(i => i.Name == oldIdx.Name))
            {
                upOps.Add($"builder.DropIndex(\"{newTable.Name}\", \"{oldIdx.Name}\");");
                downOps.Add(GenerateCreateIndex(newTable.Name, oldIdx));
            }
        }

        return (upOps, downOps);
    }

    private string MapType(string sqliteType)
    {
        return sqliteType.ToUpperInvariant() switch
        {
            "INTEGER" => "Integer",
            "TEXT" => "Text",
            "REAL" => "Real",
            "BLOB" => "Blob",
            _ => "Text" // Default to Text for unknown types
        };
    }

    private string FormatDefaultValue(string defaultValue)
    {
        // Remove quotes if present
        if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'"))
            return $"\"{defaultValue.Trim('\'')}\"";

        return $"\"{defaultValue}\"";
    }

    private string ExtractIndexColumn(string? indexSql)
    {
        if (string.IsNullOrEmpty(indexSql))
            return "id";

        // Simple extraction: find text between ( and )
        var start = indexSql.IndexOf('(');
        var end = indexSql.IndexOf(')');

        if (start >= 0 && end > start)
        {
            var columnsPart = indexSql.Substring(start + 1, end - start - 1);
            return columnsPart.Trim().Split(',')[0].Trim();
        }

        return "id";
    }

    private string GenerateMigrationFile(string migrationName, List<string> upOps, List<string> downOps)
    {
        var migrationId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var className = ToPascalCase(migrationName);

        var sb = new StringBuilder();
        sb.AppendLine("using CloudflareD1.NET.Migrations;");
        sb.AppendLine();
        sb.AppendLine("namespace YourApp.Migrations;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Migration: {className}");
        sb.AppendLine($"/// Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("/// Scaffolded from database schema");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class Migration{migrationId}_{className} : Migration");
        sb.AppendLine("{");
        sb.AppendLine($"    public override string Id => \"{migrationId}\";");
        sb.AppendLine($"    public override string Name => \"{className}\";");
        sb.AppendLine();
        sb.AppendLine("    public override void Up(MigrationBuilder builder)");
        sb.AppendLine("    {");

        if (upOps.Count == 0)
        {
            sb.AppendLine("        // No changes detected");
        }
        else
        {
            foreach (var op in upOps)
            {
                sb.AppendLine($"        {op}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down(MigrationBuilder builder)");
        sb.AppendLine("    {");

        if (downOps.Count == 0)
        {
            sb.AppendLine("        // No rollback needed");
        }
        else
        {
            foreach (var op in downOps)
            {
                sb.AppendLine($"        {op}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpper(word[0]));
                if (word.Length > 1)
                {
                    result.Append(word.Substring(1).ToLower());
                }
            }
        }

        return result.ToString();
    }
}
