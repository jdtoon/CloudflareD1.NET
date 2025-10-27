using System;
using System.Collections.Generic;

namespace CloudflareD1.NET.CodeFirst.Metadata;

/// <summary>
/// Represents metadata for an entity type
/// </summary>
public class EntityTypeMetadata
{
    /// <summary>
    /// Gets or sets the CLR type
    /// </summary>
    public Type ClrType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the table name
    /// </summary>
    public string TableName { get; set; } = null!;

    /// <summary>
    /// Gets the properties
    /// </summary>
    public List<PropertyMetadata> Properties { get; } = new();

    /// <summary>
    /// Gets the primary key properties
    /// </summary>
    public List<PropertyMetadata> PrimaryKey { get; } = new();

    /// <summary>
    /// Gets the foreign keys
    /// </summary>
    public List<ForeignKeyMetadata> ForeignKeys { get; } = new();

    /// <summary>
    /// Gets the indexes
    /// </summary>
    public List<IndexMetadata> Indexes { get; } = new();
}
