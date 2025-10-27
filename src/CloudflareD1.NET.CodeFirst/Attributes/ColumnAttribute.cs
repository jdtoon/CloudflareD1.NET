using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Specifies the database column name for a property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the database type (e.g., "TEXT", "INTEGER", "REAL", "BLOB")
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnAttribute"/> class
    /// </summary>
    /// <param name="name">The column name</param>
    public ColumnAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
