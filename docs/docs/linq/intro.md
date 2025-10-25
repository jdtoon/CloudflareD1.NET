---
sidebar_position: 1
---

# LINQ Package Overview

The **CloudflareD1.NET.Linq** package extends the core CloudflareD1.NET library with powerful LINQ query capabilities, fluent query builders, and automatic entity mapping.

## Installation

```bash
dotnet add package CloudflareD1.NET.Linq
```

:::info
Installing CloudflareD1.NET.Linq automatically includes CloudflareD1.NET as a dependency.
:::

## What's Included

### 🎯 Fluent Query Builder

Build queries with a chainable, type-safe API:

```csharp
var users = await client.Query<User>("users")
    .Where("age > ?", 18)
    .OrderBy("created_at")
    .Take(10)
    .ToListAsync();
```

### 🗺️ Entity Mapping

Automatically map query results to strongly-typed objects:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

var users = await client.QueryAsync<User>("SELECT * FROM users");
```

### 🔄 Smart Conversions

- **Snake_case to PascalCase**: Database columns like `created_at` automatically map to `CreatedAt`
- **Type Conversions**: Handles all C# primitive types, DateTime, Guid, enums, and nullable types
- **SQLite Booleans**: Automatically converts 0/1 to true/false
- **Null Handling**: Properly handles nullable database columns

### 🚀 Performance

- **Reflection Caching**: Property mappings are cached for fast repeated queries
- **Minimal Overhead**: Less than 1ms overhead for mapping 1000 rows
- **Optimized Execution**: Methods like `CountAsync()` and `AnyAsync()` use efficient SQL

## Key Features

| Feature | Description |
|---------|-------------|
| **Query Builder** | Fluent API with Where, OrderBy, Take, Skip |
| **Entity Mapping** | Automatic result-to-object conversion |
| **Generic Queries** | Type-safe query methods like `QueryAsync<T>()` |
| **Aggregates** | CountAsync(), AnyAsync() support |
| **Pagination** | Built-in Take/Skip for easy paging |
| **Custom Mappers** | Implement IEntityMapper for custom logic |
| **Parameterized Queries** | SQL injection protection with ? placeholders |

## Quick Example

```csharp
using CloudflareD1.NET.Linq;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Query with entity mapping
var products = await client.QueryAsync<Product>(
    "SELECT * FROM products WHERE is_active = @active",
    new { active = true }
);

// Query builder approach
var results = await client.Query<Product>("products")
    .Where("price >= ?", 50.00m)
    .Where("is_active = ?", true)
    .OrderByDescending("created_at")
    .Take(20)
    .ToListAsync();

// Aggregates
var count = await client.Query<Product>("products")
    .Where("is_active = ?", true)
    .CountAsync();

var hasProducts = await client.Query<Product>("products")
    .AnyAsync();
```

## Documentation Sections

- **[Installation](installation)** - Getting started with the LINQ package
- **[Query Builder](query-builder)** - Fluent query API reference
- **[Entity Mapping](entity-mapping)** - How automatic mapping works

## What's Next?

Start with the [Installation Guide](installation) to add the LINQ package to your project, then explore the [Query Builder](query-builder) documentation to learn about building type-safe queries.
