using System;
using System.Reflection;

namespace CloudflareD1.NET.CodeFirst.Metadata;

/// <summary>
/// Represents metadata for a property
/// </summary>
public class PropertyMetadata
{
    /// <summary>
    /// Gets or sets the property info
    /// </summary>
    public PropertyInfo PropertyInfo { get; set; } = null!;

    /// <summary>
    /// Gets or sets the column name
    /// </summary>
    public string ColumnName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the column type (e.g., TEXT, INTEGER)
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// Gets or sets whether the property is required (NOT NULL)
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this is a primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets whether this is an auto-increment column
    /// </summary>
    public bool IsAutoIncrement { get; set; }

    /// <summary>
    /// Gets or sets the maximum length (for strings)
    /// </summary>
    public int? MaxLength { get; set; }
}
