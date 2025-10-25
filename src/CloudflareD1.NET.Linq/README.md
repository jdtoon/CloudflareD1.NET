# CloudflareD1.NET.Linq

LINQ query builder and object mapping extensions for CloudflareD1.NET. Provides type-safe query construction, automatic entity mapping, and fluent API for building SQL queries.

## Installation

```bash
dotnet add package CloudflareD1.NET.Linq
```

**Note:** This package automatically includes `CloudflareD1.NET` as a dependency.

## Features

- ✅ **Generic query methods** - `QueryAsync<T>()`, `QueryFirstOrDefaultAsync<T>()`, etc.
- ✅ **Automatic entity mapping** - Maps query results to strongly-typed objects
- ✅ **Snake_case to PascalCase conversion** - Automatically maps database columns to C# properties
- ✅ **Nullable type support** - Handles nullable properties correctly
- ✅ **Custom mappers** - Implement `IEntityMapper` for custom mapping logic
- ✅ **Performance optimized** - Uses reflection caching for fast mapping

## Quick Start

### 1. Define Your Entities

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 2. Query with Type Safety

```csharp
using CloudflareD1.NET.Linq;

// Query all users
var users = await client.QueryAsync<User>("SELECT * FROM users");

// Query with parameters
var activeUsers = await client.QueryAsync<User>(
    "SELECT * FROM users WHERE is_active = @active",
    new { active = true }
);

// Get single user
var user = await client.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE id = @id",
    new { id = 123 }
);
```

### 3. Handle Nulls and Conversions

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }  // Nullable
    public DateTime PublishedAt { get; set; }
    public int ViewCount { get; set; }
}

var articles = await client.QueryAsync<Article>(
    "SELECT * FROM articles WHERE published_at > @date",
    new { date = DateTime.UtcNow.AddDays(-7) }
);
```

## Advanced Usage

### Custom Entity Mapper

```csharp
using CloudflareD1.NET.Linq.Mapping;

public class CustomMapper : IEntityMapper
{
    public T Map<T>(Dictionary<string, object?> row) where T : new()
    {
        // Your custom mapping logic
        var entity = new T();
        // ... populate entity from row
        return entity;
    }

    public IEnumerable<T> MapMany<T>(IEnumerable<Dictionary<string, object?>> rows) where T : new()
    {
        return rows.Select(Map<T>);
    }
}

// Use custom mapper
var users = await client.QueryAsync<User>(
    "SELECT * FROM users",
    mapper: new CustomMapper()
);
```

### Column Name Mapping

The default mapper automatically handles snake_case to PascalCase conversion:

```csharp
// Database columns: user_id, first_name, created_at, is_active
// C# properties:    UserId, FirstName, CreatedAt, IsActive

public class User
{
    public int UserId { get; set; }        // Maps from user_id
    public string FirstName { get; set; }  // Maps from first_name
    public DateTime CreatedAt { get; set; } // Maps from created_at
    public bool IsActive { get; set; }     // Maps from is_active
}
```

### Query Methods

```csharp
// Get collection of entities
var users = await client.QueryAsync<User>("SELECT * FROM users");

// Get first result or null
var user = await client.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE email = @email",
    new { email = "test@example.com" }
);

// Get exactly one result (throws if 0 or >1 results)
var user = await client.QuerySingleAsync<User>(
    "SELECT * FROM users WHERE id = @id",
    new { id = 123 }
);

// Get one result or null (throws if >1 results)
var user = await client.QuerySingleOrDefaultAsync<User>(
    "SELECT * FROM users WHERE email = @email",
    new { email = "test@example.com" }
);
```

## Type Conversions

The mapper handles these conversions automatically:

- **Numeric types** - int, long, double, decimal, float
- **Boolean** - Handles SQLite's 0/1 integer representation
- **DateTime/DateTimeOffset** - Parses ISO 8601 strings
- **Guid** - Parses string representations
- **Enums** - Parses from string or integer values
- **Nullable types** - All types support nullable variants

## Performance

The mapper uses aggressive caching to minimize reflection overhead:

- **Property metadata** cached per type
- **Column-to-property mappings** cached
- **Type converters** optimized for common scenarios

Benchmark results show <1ms overhead for mapping 1000 rows on typical hardware.

## Examples

### E-commerce Query

```csharp
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

var recentOrders = await client.QueryAsync<Order>(@"
    SELECT id, user_id, total_amount, status, created_at
    FROM orders
    WHERE user_id = @userId
    AND created_at > @since
    ORDER BY created_at DESC
    LIMIT @limit
", new {
    userId = 123,
    since = DateTime.UtcNow.AddMonths(-1),
    limit = 10
});
```

### Aggregate Query

```csharp
public class UserStats
{
    public int UserId { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime FirstOrder { get; set; }
}

var stats = await client.QueryAsync<UserStats>(@"
    SELECT 
        user_id,
        COUNT(*) as order_count,
        SUM(total_amount) as total_spent,
        MIN(created_at) as first_order
    FROM orders
    GROUP BY user_id
    HAVING order_count > @minOrders
", new { minOrders = 5 });
```

## Coming Soon

- 🚧 **Query Builder** - Fluent API with LINQ expressions
- 🚧 **Where/OrderBy/Take** - Type-safe query construction
- 🚧 **Join support** - Multi-table queries
- 🚧 **Group By/Aggregates** - Complex grouping operations

## Documentation

For complete documentation, visit: https://github.com/jdtoon/CloudflareD1.NET

## License

MIT License - see LICENSE file for details
