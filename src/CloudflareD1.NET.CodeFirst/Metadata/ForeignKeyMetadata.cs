using System;
using System.Collections.Generic;

namespace CloudflareD1.NET.CodeFirst.Metadata;

/// <summary>
/// Represents metadata for a foreign key
/// </summary>
public class ForeignKeyMetadata
{
    /// <summary>
    /// Gets or sets the principal entity type (referenced table)
    /// </summary>
    public Type PrincipalType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the dependent entity type (referencing table)
    /// </summary>
    public Type DependentType { get; set; } = null!;

    /// <summary>
    /// Gets the foreign key properties (in dependent entity)
    /// </summary>
    public List<PropertyMetadata> DependentProperties { get; } = new();

    /// <summary>
    /// Gets the principal key properties (in principal entity)
    /// </summary>
    public List<PropertyMetadata> PrincipalProperties { get; } = new();

    /// <summary>
    /// Gets or sets the foreign key constraint name
    /// </summary>
    public string? ConstraintName { get; set; }

    /// <summary>
    /// Gets or sets the delete behavior (CASCADE, SET NULL, etc.)
    /// </summary>
    public string? OnDelete { get; set; }
}
