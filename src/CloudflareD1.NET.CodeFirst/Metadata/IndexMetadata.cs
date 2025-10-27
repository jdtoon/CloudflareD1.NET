using System.Collections.Generic;

namespace CloudflareD1.NET.CodeFirst.Metadata;

/// <summary>
/// Represents metadata for an index
/// </summary>
public class IndexMetadata
{
    /// <summary>
    /// Gets or sets the index name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets the properties included in the index
    /// </summary>
    public List<PropertyMetadata> Properties { get; } = new();

    /// <summary>
    /// Gets or sets whether the index is unique
    /// </summary>
    public bool IsUnique { get; set; }
}
