using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudflareD1.NET.Migrations
{
    /// <summary>
    /// Fluent API for building table definitions.
    /// </summary>
    public class TableBuilder
    {
        private readonly string _tableName;
        private readonly List<ColumnDefinition> _columns = new List<ColumnDefinition>();
        private readonly List<string> _constraints = new List<string>();

        internal TableBuilder(string tableName)
        {
            _tableName = tableName;
        }

        /// <summary>
        /// Adds an INTEGER column.
        /// </summary>
        public ColumnBuilder Integer(string columnName)
        {
            return AddColumn(columnName, "INTEGER");
        }

        /// <summary>
        /// Adds a TEXT column.
        /// </summary>
        public ColumnBuilder Text(string columnName)
        {
            return AddColumn(columnName, "TEXT");
        }

        /// <summary>
        /// Adds a REAL (floating point) column.
        /// </summary>
        public ColumnBuilder Real(string columnName)
        {
            return AddColumn(columnName, "REAL");
        }

        /// <summary>
        /// Adds a BLOB column.
        /// </summary>
        public ColumnBuilder Blob(string columnName)
        {
            return AddColumn(columnName, "BLOB");
        }

        /// <summary>
        /// Adds a column with custom type.
        /// </summary>
        public ColumnBuilder Column(string columnName, string columnType)
        {
            return AddColumn(columnName, columnType);
        }

        private ColumnBuilder AddColumn(string columnName, string columnType)
        {
            var column = new ColumnDefinition
            {
                Name = columnName,
                Type = columnType
            };
            _columns.Add(column);
            return new ColumnBuilder(column);
        }

        /// <summary>
        /// Adds a PRIMARY KEY constraint on one or more columns.
        /// </summary>
        public TableBuilder PrimaryKey(params string[] columns)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Primary key columns cannot be null or empty.", nameof(columns));

            var columnList = string.Join(", ", columns);
            _constraints.Add($"PRIMARY KEY ({columnList})");
            return this;
        }

        /// <summary>
        /// Adds a UNIQUE constraint on one or more columns.
        /// </summary>
        public TableBuilder Unique(params string[] columns)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Unique constraint columns cannot be null or empty.", nameof(columns));

            var columnList = string.Join(", ", columns);
            _constraints.Add($"UNIQUE ({columnList})");
            return this;
        }

        /// <summary>
        /// Adds a FOREIGN KEY constraint.
        /// </summary>
        public TableBuilder ForeignKey(string column, string referencedTable, string referencedColumn, string? onDelete = null, string? onUpdate = null)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column cannot be null or empty.", nameof(column));
            if (string.IsNullOrWhiteSpace(referencedTable))
                throw new ArgumentException("Referenced table cannot be null or empty.", nameof(referencedTable));
            if (string.IsNullOrWhiteSpace(referencedColumn))
                throw new ArgumentException("Referenced column cannot be null or empty.", nameof(referencedColumn));

            var constraint = new StringBuilder($"FOREIGN KEY ({column}) REFERENCES {referencedTable}({referencedColumn})");

            if (!string.IsNullOrWhiteSpace(onDelete))
                constraint.Append($" ON DELETE {onDelete}");

            if (!string.IsNullOrWhiteSpace(onUpdate))
                constraint.Append($" ON UPDATE {onUpdate}");

            _constraints.Add(constraint.ToString());
            return this;
        }

        /// <summary>
        /// Adds a CHECK constraint.
        /// </summary>
        public TableBuilder Check(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Check expression cannot be null or empty.", nameof(expression));

            _constraints.Add($"CHECK ({expression})");
            return this;
        }

        internal string BuildCreateTableSql()
        {
            var sql = new StringBuilder($"CREATE TABLE {_tableName} (\n");

            // Add columns
            var columnDefinitions = _columns.Select(c => $"  {c.ToSql()}");
            sql.Append(string.Join(",\n", columnDefinitions));

            // Add table constraints
            if (_constraints.Count > 0)
            {
                sql.Append(",\n");
                sql.Append(string.Join(",\n", _constraints.Select(c => $"  {c}")));
            }

            sql.Append("\n)");

            return sql.ToString();
        }
    }

    /// <summary>
    /// Fluent API for building column definitions.
    /// </summary>
    public class ColumnBuilder
    {
        private readonly ColumnDefinition _column;

        internal ColumnBuilder(ColumnDefinition column)
        {
            _column = column;
        }

        /// <summary>
        /// Marks the column as NOT NULL.
        /// </summary>
        public ColumnBuilder NotNull()
        {
            _column.Nullable = false;
            return this;
        }

        /// <summary>
        /// Marks the column as PRIMARY KEY.
        /// </summary>
        public ColumnBuilder PrimaryKey()
        {
            _column.IsPrimaryKey = true;
            return this;
        }

        /// <summary>
        /// Marks the column as AUTOINCREMENT (only for INTEGER PRIMARY KEY).
        /// </summary>
        public ColumnBuilder AutoIncrement()
        {
            _column.AutoIncrement = true;
            return this;
        }

        /// <summary>
        /// Marks the column as UNIQUE.
        /// </summary>
        public ColumnBuilder Unique()
        {
            _column.IsUnique = true;
            return this;
        }

        /// <summary>
        /// Sets a default value for the column.
        /// </summary>
        public ColumnBuilder Default(object value)
        {
            _column.DefaultValue = value;
            return this;
        }
    }

    internal class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Nullable { get; set; } = true;
        public bool IsPrimaryKey { get; set; }
        public bool AutoIncrement { get; set; }
        public bool IsUnique { get; set; }
        public object? DefaultValue { get; set; }

        public string ToSql()
        {
            var sql = new StringBuilder($"{Name} {Type}");

            if (IsPrimaryKey)
                sql.Append(" PRIMARY KEY");

            if (AutoIncrement)
                sql.Append(" AUTOINCREMENT");

            if (!Nullable)
                sql.Append(" NOT NULL");

            if (IsUnique && !IsPrimaryKey)
                sql.Append(" UNIQUE");

            if (DefaultValue != null)
            {
                var defaultStr = DefaultValue is string
                    ? $"'{DefaultValue}'"
                    : DefaultValue.ToString();
                sql.Append($" DEFAULT {defaultStr}");
            }

            return sql.ToString();
        }
    }
}
