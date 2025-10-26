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

## What's New

### v1.3.0 - IQueryable&lt;T&gt; with Deferred Execution

Standard LINQ query syntax with deferred execution:

```csharp
// Create IQueryable - query is NOT executed yet
IQueryable<User> queryable = client.AsQueryable<User>("users");

// Compose query - still not executed (deferred execution)
var adults = queryable
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .Skip(10)
    .Take(5);

// NOW the query executes
var results = await ((D1Queryable<User>)adults).ToListAsync();
```

**Supported Operations:**
- ✅ **Where** - Filter with lambda expressions, multiple clauses combined with AND
- ✅ **OrderBy / OrderByDescending** - Sort by properties
- ✅ **ThenBy / ThenByDescending** - Secondary sorting
- ✅ **Take / Skip** - Pagination
- ✅ **Terminal operations** - ToListAsync, CountAsync, FirstOrDefaultAsync, SingleAsync, AnyAsync

**Key Benefits:**
- ✅ **Deferred execution** - Query only runs when enumerated
- ✅ **Composable** - Build queries incrementally and reuse query fragments
- ✅ **Standard LINQ** - Use familiar IQueryable&lt;T&gt; patterns
- ✅ **Testable** - Easy to unit test query composition logic

### v1.2.1 - Computed Properties in Select()

Generate new values dynamically using expressions:

```csharp
var usersWithFlags = await client.Query<User>("users")
    .Select(u => new {
        u.Id,
        u.Name,
        u.Age,
        IsAdult = u.Age >= 18,
        YearsUntil65 = 65 - u.Age,
        Total = u.Price * u.Quantity
    })
    .ToListAsync();
```

**Supported Operations:**
- ✅ **Boolean expressions** - `u.Age >= 18`, `u.Price > 100`
- ✅ **Math operations** - `u.Price * u.Quantity`, `65 - u.Age`
- ✅ **Comparisons** - `>`, `<`, `>=`, `<=`, `==`, `!=`
- ✅ **String methods** - `u.Name.ToUpper()`, `u.Email.ToLower()`

### v1.2.0 - Select() Projection

Select specific columns and transform results:

```csharp
var summaries = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
    .OrderBy("name")
    .ToListAsync();
```

**Benefits:**
- ✅ **Reduced data transfer** - Only fetch columns you need
- ✅ **Type-safe DTOs** - Project to strongly-typed classes
- ✅ **Better performance** - Less data over the network
- ✅ **Cleaner code** - Express intent clearly

### v1.1.0 - Expression Tree LINQ

Write type-safe queries using lambda expressions with full IntelliSense:

```csharp
// Expression-based queries (v1.1.0+)
var adults = await client.Query<User>("users")
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToListAsync();

// String-based queries (still supported)
var adults = await client.Query<User>("users")
    .Where("age >= ?", 18)
    .Where("is_active = ?", true)
    .OrderBy("name")
    .Take(10)
    .ToListAsync();
```

**Benefits:**
- ✅ **Compile-time type checking** - Catch errors before runtime
- ✅ **IntelliSense support** - Full autocomplete for properties
- ✅ **Refactoring support** - Rename properties safely
- ✅ **No SQL injection** - Parameters automatically handled
- ✅ **Backward compatible** - String-based queries still work

## What's Included

### 🎯 Fluent Query Builder

Build queries with a chainable, type-safe API supporting both expression trees and SQL strings:

```csharp
// Expression-based (type-safe)
var users = await client.Query<User>("users")
    .Where(u => u.Age > 18)
    .OrderBy(u => u.CreatedAt)
    .Take(10)
    .ToListAsync();

// String-based (flexible)
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
| **Expression Trees (v1.1.0+)** | Lambda expressions with compile-time type checking |
| **Query Builder** | Fluent API with Where, OrderBy, Take, Skip |
| **Entity Mapping** | Automatic result-to-object conversion |
| **Generic Queries** | Type-safe query methods like `QueryAsync<T>()` |
| **Aggregates** | CountAsync(), AnyAsync() support |
| **Pagination** | Built-in Take/Skip for easy paging |
| **Custom Mappers** | Implement IEntityMapper for custom logic |
| **Parameterized Queries** | SQL injection protection with ? placeholders |

## Supported Expression Features (v1.1.0+)

The expression tree parser supports:

- **Comparison operators**: `>`, `<`, `>=`, `<=`, `==`, `!=`
- **Logical operators**: `&&` (AND), `||` (OR), `!` (NOT)
- **Null checks**: `!= null` becomes `IS NOT NULL`
- **String methods**: `Contains()`, `StartsWith()`, `EndsWith()`, `ToLower()`, `ToUpper()`
- **Math operators**: `+`, `-`, `*`, `/`
- **Captured variables**: Automatically extracts values from closure scope

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
