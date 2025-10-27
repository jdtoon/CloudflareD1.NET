using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Specifies the database table name for an entity
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the table
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableAttribute"/> class
    /// </summary>
    /// <param name="name">The table name</param>
    public TableAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
