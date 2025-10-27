using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Migrations;

/// <summary>
/// Introspects SQLite database schema to extract table and column information
/// </summary>
public class SchemaIntrospector
{
    private readonly D1Client _client;

    public SchemaIntrospector(D1Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Gets the complete database schema including tables, columns, and indexes
    /// </summary>
    public async Task<DatabaseSchema> GetSchemaAsync()
    {
        var schema = new DatabaseSchema();

        // Get all tables (excluding sqlite internal tables)
        var tablesResult = await _client.QueryAsync(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__migrations' ORDER BY name");

        if (tablesResult.Results == null) return schema;

        foreach (var row in tablesResult.Results)
        {
            if (!row.TryGetValue("name", out var nameObj) || nameObj == null) continue;
            var tableName = nameObj.ToString()!;

            var table = new TableSchema { Name = tableName };

            // Get columns for this table
            var columnsResult = await _client.QueryAsync($"PRAGMA table_info({tableName})");
            if (columnsResult.Results != null)
            {
                foreach (var colRow in columnsResult.Results)
                {
                    table.Columns.Add(new ColumnSchema
                    {
                        Name = colRow.TryGetValue("name", out var n) && n != null ? n.ToString()! : "",
                        Type = colRow.TryGetValue("type", out var t) && t != null ? t.ToString()! : "",
                        NotNull = colRow.TryGetValue("notnull", out var nn) && nn != null && Convert.ToInt32(nn) == 1,
                        DefaultValue = colRow.TryGetValue("dflt_value", out var dv) && dv != null ? dv.ToString() : null,
                        IsPrimaryKey = colRow.TryGetValue("pk", out var pk) && pk != null && Convert.ToInt32(pk) == 1
                    });
                }
            }

            // Get indexes for this table
            var indexesResult = await _client.QueryAsync(
                $"SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='{tableName}' AND sql IS NOT NULL");
            if (indexesResult.Results != null)
            {
                foreach (var idxRow in indexesResult.Results)
                {
                    table.Indexes.Add(new IndexSchema
                    {
                        Name = idxRow.TryGetValue("name", out var n) && n != null ? n.ToString()! : "",
                        Sql = idxRow.TryGetValue("sql", out var s) && s != null ? s.ToString() : null
                    });
                }
            }

            schema.Tables.Add(table);
        }

        return schema;
    }
}

/// <summary>
/// Represents a complete database schema
/// </summary>
public class DatabaseSchema
{
    public List<TableSchema> Tables { get; set; } = new();
}

/// <summary>
/// Represents a table schema
/// </summary>
public class TableSchema
{
    public string Name { get; set; } = string.Empty;
    public List<ColumnSchema> Columns { get; set; } = new();
    public List<IndexSchema> Indexes { get; set; } = new();
}

/// <summary>
/// Represents a column schema
/// </summary>
public class ColumnSchema
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool NotNull { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
}

/// <summary>
/// Represents an index schema
/// </summary>
public class IndexSchema
{
    public string Name { get; set; } = string.Empty;
    public string? Sql { get; set; }
}
