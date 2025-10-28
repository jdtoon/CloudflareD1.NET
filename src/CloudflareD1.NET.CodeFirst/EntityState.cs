using System;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Represents the tracking state of an entity
/// </summary>
public enum EntityState
{
    Detached = 0,
    Unchanged = 1,
    Added = 2,
    Modified = 3,
    Deleted = 4
}
