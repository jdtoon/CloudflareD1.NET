using System;

namespace CloudflareD1.NET.CodeFirst.Attributes;

/// <summary>
/// Specifies that a column must have a value (NOT NULL constraint)
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class RequiredAttribute : Attribute
{
}
