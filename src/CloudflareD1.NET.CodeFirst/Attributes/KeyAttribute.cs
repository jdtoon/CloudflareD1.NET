using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Denotes that a property is part of the primary key
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class KeyAttribute : Attribute
{
}
