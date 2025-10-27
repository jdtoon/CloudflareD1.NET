using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Denotes that a property should not be mapped to a database column
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NotMappedAttribute : Attribute
{
}
