using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Specifies an index for an entity. Can be applied multiple times to define multiple indexes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IndexAttribute : Attribute
{
    /// <summary>
    /// Gets the property names that make up the index
    /// </summary>
    public string[] PropertyNames { get; }

    /// <summary>
    /// Gets or sets whether the index is unique
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Gets or sets the index name. If not specified, a name will be generated.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexAttribute"/> class for a single-column index
    /// </summary>
    /// <param name="propertyName">The property name to index</param>
    public IndexAttribute(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or whitespace", nameof(propertyName));

        PropertyNames = new[] { propertyName };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexAttribute"/> class for a composite index
    /// </summary>
    /// <param name="propertyNames">The property names to index</param>
    public IndexAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length == 0)
            throw new ArgumentException("At least one property name is required", nameof(propertyNames));

        PropertyNames = propertyNames;
    }
}
