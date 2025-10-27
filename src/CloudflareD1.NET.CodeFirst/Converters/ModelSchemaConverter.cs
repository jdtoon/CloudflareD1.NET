using System;
using System.Linq;
using CloudflareD1.NET.CodeFirst.Metadata;
using CloudflareD1.NET.Migrations;

namespace CloudflareD1.NET.CodeFirst.Converters;

/// <summary>
/// Converts Code-First model metadata into a DatabaseSchema compatible with MigrationScaffolder
/// </summary>
public static class ModelSchemaConverter
{
    public static DatabaseSchema ToDatabaseSchema(ModelMetadata model)
    {
        var db = new DatabaseSchema();

        foreach (var entityKvp in model.Entities)
        {
            var entity = entityKvp.Value;
            var table = new TableSchema
            {
                Name = entity.TableName
            };

            foreach (var prop in entity.Properties)
            {
                var col = new ColumnSchema
                {
                    Name = prop.ColumnName,
                    Type = prop.ColumnType ?? InferSqlType(prop),
                    NotNull = prop.IsRequired || prop.IsPrimaryKey,
                    IsPrimaryKey = prop.IsPrimaryKey
                };
                table.Columns.Add(col);
            }

            // Foreign keys (if any)
            foreach (var fk in entity.ForeignKeys)
            {
                // Resolve dependent (FK) column name
                var fkProp = fk.DependentProperties.FirstOrDefault();
                if (fkProp == null) continue;

                // Resolve principal table and key column
                var principal = model.GetEntity(fk.PrincipalType);
                var principalTable = principal?.TableName ?? ModelSchemaConverterHelpers.ToSnakeCase(ModelSchemaConverterHelpers.Pluralize(fk.PrincipalType.Name));
                var principalKeyCol = principal?.PrimaryKey.FirstOrDefault()?.ColumnName ?? "id";

                table.ForeignKeys.Add(new ForeignKeySchema
                {
                    Column = fkProp.ColumnName,
                    ReferencedTable = principalTable,
                    ReferencedColumn = principalKeyCol,
                    OnDelete = fk.OnDelete
                });
            }

            // Indexes (if any)
            foreach (var idx in entity.Indexes)
            {
                // Build column list
                var columns = idx.Properties.Select(p => p.ColumnName).ToArray();
                if (columns.Length == 0) continue;

                // Generate CREATE INDEX SQL
                var unique = idx.IsUnique ? "UNIQUE " : "";
                var columnList = string.Join(", ", columns);
                var sql = $"CREATE {unique}INDEX {idx.Name} ON {entity.TableName} ({columnList})";

                table.Indexes.Add(new IndexSchema
                {
                    Name = idx.Name,
                    Sql = sql
                });
            }

            db.Tables.Add(table);
        }

        return db;
    }

    private static string InferSqlType(PropertyMetadata prop)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyInfo.PropertyType) ?? prop.PropertyInfo.PropertyType;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(bool))
            return "INTEGER";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return "REAL";
        if (type == typeof(byte[]))
            return "BLOB";
        return "TEXT";
    }
}

internal static class ModelSchemaConverterHelpers
{
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder();
        sb.Append(char.ToLowerInvariant(input[0]));
        for (int i = 1; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c)) { sb.Append('_'); sb.Append(char.ToLowerInvariant(c)); }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Pluralize(string name)
    {
        // very naive pluralization for now
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase)) return name;
        return name + "s";
    }
}
