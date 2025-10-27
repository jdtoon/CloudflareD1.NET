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

            // Indexes are not yet modeled in CodeFirst MVP

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
