using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Specifies the foreign key relationship for a navigation property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ForeignKeyAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the foreign key property
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyAttribute"/> class
    /// </summary>
    /// <param name="name">The name of the foreign key property</param>
    public ForeignKeyAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
