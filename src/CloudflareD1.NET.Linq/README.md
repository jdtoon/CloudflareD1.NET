# CloudflareD1.NET.Linq

LINQ query builder and object mapping extensions for CloudflareD1.NET. Provides type-safe query construction, automatic entity mapping, and fluent API for building SQL queries.

## Installation

```bash
dotnet add package CloudflareD1.NET.Linq
```

**Note:** This package automatically includes `CloudflareD1.NET` as a dependency.

## Features

- ✅ **GroupBy & Aggregations** - Group results with Count, Sum, Average, Min, Max (NEW in v1.5.0)
- ✅ **IQueryable<T> support** - Standard LINQ query syntax with deferred execution (v1.3.0+)
- ✅ **Select() projections** - Project to DTOs with computed properties (v1.4.0+)
- ✅ **Fluent query builder** - Chain methods like `.Where()`, `.OrderBy()`, `.Take()`, `.Skip()`
- ✅ **Generic query methods** - `QueryAsync<T>()`, `QueryFirstOrDefaultAsync<T>()`, etc.
- ✅ **Automatic entity mapping** - Maps query results to strongly-typed objects
- ✅ **Snake_case to PascalCase conversion** - Automatically maps database columns to C# properties
- ✅ **Parameterized queries** - Safe from SQL injection with `?` placeholders
- ✅ **Pagination support** - Easy `Take()` and `Skip()` for paging
- ✅ **Aggregate functions** - `CountAsync()`, `AnyAsync()`
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

### 2. Use the Fluent Query Builder

```csharp
using CloudflareD1.NET.Linq;

// Simple query with filtering
var activeUsers = await client.Query<User>("users")
    .Where("is_active = ?", true)
    .OrderBy("name")
    .ToListAsync();

// Pagination
var page2Users = await client.Query<User>("users")
    .OrderBy("created_at")
    .Skip(20)
    .Take(10)
    .ToListAsync();

// Complex filtering
var recentUsers = await client.Query<User>("users")
    .Where("created_at > ?", DateTime.UtcNow.AddDays(-7))
    .Where("is_active = ?", true)
    .OrderByDescending("created_at")
    .Take(50)
    .ToListAsync();

// Aggregates
var userCount = await client.Query<User>("users")
    .Where("email LIKE ?", "%@example.com")
    .CountAsync();

var hasUsers = await client.Query<User>("users")
    .Where("is_active = ?", true)
    .AnyAsync();

// Single result
var user = await client.Query<User>("users")
    .Where("id = ?", 123)
    .SingleOrDefaultAsync();
```

### 3. IQueryable<T> with Deferred Execution (NEW in v1.3.0)

Use standard LINQ query syntax with `AsQueryable<T>()` for deferred execution:

```csharp
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;

// Create IQueryable - query is NOT executed yet
IQueryable<User> queryable = client.AsQueryable<User>("users");

// Compose query - still not executed (deferred execution)
var adults = queryable
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name);

// NOW the query executes when we enumerate
var results = await ((D1Queryable<User>)adults).ToListAsync();

// Multiple Where clauses (combined with AND)
var youngAdults = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)
    .Where(u => u.Age < 30);
var youngAdultList = await ((D1Queryable<User>)youngAdults).ToListAsync();

// Pagination with IQueryable
var pagedQuery = client.AsQueryable<User>("users")
    .OrderBy(u => u.Id)
    .Skip(10)
    .Take(5);
var pagedResults = await ((D1Queryable<User>)pagedQuery).ToListAsync();

// Complex query composition
var complexQuery = client.AsQueryable<User>("users")
    .Where(u => u.Age > 20)
    .OrderBy(u => u.Name)
    .Skip(5)
    .Take(10);
var complexResults = await ((D1Queryable<User>)complexQuery).ToListAsync();

// Count with filtering
var countQuery = client.AsQueryable<User>("users")
    .Where(u => u.IsActive);
var activeCount = await ((D1Queryable<User>)countQuery).CountAsync();

// FirstOrDefaultAsync
var firstQuery = client.AsQueryable<User>("users")
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name);
var firstUser = await ((D1Queryable<User>)firstQuery).FirstOrDefaultAsync();

// AnyAsync
var anyQuery = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 65);
var hasSeniors = await ((D1Queryable<User>)anyQuery).AnyAsync();
```

**Select() Projections (NEW in v1.4.0):**

Project query results into DTOs or custom types:

```csharp
// Define a DTO
public class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Simple projection
var summaries = client.AsQueryable<User>("users")
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
var results = await ((D1ProjectionQueryable<UserSummary>)summaries).ToListAsync();

// With computed properties
public class UserWithAge
{
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsAdult { get; set; }
}

var withComputed = client.AsQueryable<User>("users")
    .Select(u => new UserWithAge
    {
        Name = u.Name,
        Age = u.Age,
        IsAdult = u.Age >= 18  // Computed
    });
var computed = await ((D1ProjectionQueryable<UserWithAge>)withComputed).ToListAsync();

// Combine with filtering and sorting
var adultSummaries = client.AsQueryable<User>("users")
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
var filtered = await ((D1ProjectionQueryable<UserSummary>)adultSummaries).ToListAsync();

// With pagination
var paged = client.AsQueryable<User>("users")
    .OrderBy(u => u.Id)
    .Skip(10)
    .Take(5)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
var pageResults = await ((D1ProjectionQueryable<UserSummary>)paged).ToListAsync();
```

**Key Benefits of IQueryable:**
- **Deferred Execution** - Query only runs when you enumerate (ToListAsync, CountAsync, etc.)
- **Composable** - Build queries incrementally and reuse query fragments
- **Standard LINQ** - Use familiar LINQ methods: Where, OrderBy, OrderByDescending, Take, Skip
- **Testable** - Easy to unit test query composition logic

### 4. Direct SQL Queries with Type Mapping

```csharp
// Query all users
var users = await client.QueryAsync<User>("SELECT * FROM users");

// Query with named parameters
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

## Query Builder API

### Filtering

```csharp
// Single WHERE clause with positional parameters
.Where("age > ?", 18)

// Multiple WHERE clauses (combined with AND)
.Where("age > ?", 18)
.Where("country = ?", "US")

// LIKE queries
.Where("email LIKE ?", "%@example.com")

// Complex conditions
.Where("(age > ? OR premium = ?) AND country = ?", 18, true, "US")
```

### Sorting

```csharp
// Single column ascending
.OrderBy("name")

// Single column descending
.OrderByDescending("created_at")

// Multiple columns
.OrderBy("country")
.ThenBy("city")
.ThenByDescending("created_at")
```

### Pagination

```csharp
// Skip first 20, take next 10
.Skip(20)
.Take(10)

// First page (10 per page)
.Take(10)

// Second page
.Skip(10)
.Take(10)

// Typical pagination pattern
int page = 2;
int pageSize = 10;
var results = await client.Query<User>("users")
    .OrderBy("id")
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### Execution Methods

```csharp
// Get all matching results
var list = await query.ToListAsync();

// Get first result or null
var first = await query.FirstOrDefaultAsync();

// Get exactly one result (throws if 0 or >1)
var single = await query.SingleAsync();

// Get exactly one result or null (throws if >1)
var singleOrNull = await query.SingleOrDefaultAsync();

// Get count of matching records
var count = await query.CountAsync();

// Check if any records match
var exists = await query.AnyAsync();
```

### Select() Projection with Computed Properties (v1.2.1+)

Select specific columns and compute new values on-the-fly:

```csharp
// DTO class for projection
public class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsAdult { get; set; }
    public int YearsUntil65 { get; set; }
}

// Boolean computed properties
var adults = await client.Query<User>("users")
    .Select(u => new UserSummary { 
        Id = u.Id, 
        Name = u.Name,
        IsAdult = u.Age >= 18,
        YearsUntil65 = 65 - u.Age
    })
    .ToListAsync();

// Math operations
var orderSummary = await client.Query<Order>("orders")
    .Select(o => new {
        o.Id,
        Total = o.Price * o.Quantity,
        Discount = o.Price * 0.1m,
        FinalPrice = (o.Price * o.Quantity) - (o.Price * 0.1m)
    })
    .ToListAsync();

// Comparisons and boolean logic
var userFlags = await client.Query<User>("users")
    .Select(u => new {
        u.Id,
        u.Name,
        u.Age,
        IsAdult = u.Age >= 18,
        IsMinor = u.Age < 18,
        IsSenior = u.Age >= 65,
        IsExpensive = u.MonthlyFee > 100
    })
    .ToListAsync();

// String methods
var formattedUsers = await client.Query<User>("users")
    .Select(u => new {
        u.Id,
        UpperName = u.Name.ToUpper(),
        LowerEmail = u.Email.ToLower()
    })
    .ToListAsync();
```

**Supported Operations:**
- **Comparisons**: `>`, `<`, `>=`, `<=`, `==`, `!=`
- **Math**: `+`, `-`, `*`, `/`
- **Boolean logic**: `&&` (AND), `||` (OR), `!` (NOT)
- **String methods**: `ToUpper()`, `ToLower()`, `Contains()`, `StartsWith()`, `EndsWith()`

### GroupBy & Aggregations (NEW in v1.5.0)

Group query results and perform aggregate calculations:

```csharp
// Group by single column with Count()
var usersByAge = await client.Query<User>("users")
    .GroupBy(u => u.Age)
    .Select(g => new AgeGroup
    {
        Age = g.Key,
        UserCount = g.Count()
    })
    .ToListAsync();

// Multiple aggregates
var salesByCategory = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryStats
    {
        Category = g.Key,
        ProductCount = g.Count(),
        TotalPrice = g.Sum(p => p.Price),
        AveragePrice = g.Average(p => p.Price),
        MinPrice = g.Min(p => p.Price),
        MaxPrice = g.Max(p => p.Price)
    })
    .ToListAsync();

// GroupBy with Where (filters before grouping)
var activeUsersByAge = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .GroupBy(u => u.Age)
    .Select(g => new { Age = g.Key, Count = g.Count() })
    .ToListAsync();

// GroupBy with OrderBy and Take
var topCategories = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryCount
    {
        Category = g.Key,
        Count = g.Count()
    })
    .OrderByDescending("count")
    .Take(10)
    .ToListAsync();

// Complex aggregate expressions
var orderTotals = await client.Query<Order>("orders")
    .GroupBy(o => o.CustomerId)
    .Select(g => new CustomerTotal
    {
        CustomerId = g.Key,
        TotalValue = g.Sum(o => o.Price * o.Quantity)  // Math in aggregates
    })
    .ToListAsync();
```

**Supported Aggregate Functions:**
- `Count()` - Count of items in group
- `Sum(x => x.Property)` - Sum of values
- `Average(x => x.Property)` - Average of values
- `Min(x => x.Property)` - Minimum value
- `Max(x => x.Property)` - Maximum value

**Generates SQL:**
```sql
SELECT category, COUNT(*) AS product_count, 
       SUM(price) AS total_price, AVG(price) AS average_price
FROM products
GROUP BY category
```

## Advanced Usage

### Custom Entity Mapper

Create a custom mapper for special mapping logic:

```csharp
public class CustomUserMapper : IEntityMapper
{
    public T Map<T>(Dictionary<string, object?> row)
    {
        if (typeof(T) == typeof(User))
        {
            var user = new User
            {
                Id = Convert.ToInt32(row["user_id"]),
                Name = row["full_name"]?.ToString() ?? "",
                Email = row["email_address"]?.ToString()
            };
            return (T)(object)user;
        }
        throw new NotSupportedException($"Type {typeof(T)} not supported");
    }

    public IEnumerable<T> MapMany<T>(IEnumerable<Dictionary<string, object?>> rows)
    {
        return rows.Select(Map<T>);
    }
}

// Use custom mapper
var users = await client.QueryAsync<User>(
    "SELECT * FROM users",
    parameters: null,
    mapper: new CustomUserMapper()
);

// Or with query builder
var users = await client.Query<User>("users", new CustomUserMapper())
    .Where("is_active = ?", true)
    .ToListAsync();
```

### Type Conversions

The default mapper automatically handles:

- **Primitives**: `int`, `long`, `decimal`, `float`, `double`, `bool`, `byte`, `short`
- **Strings**: Direct assignment
- **DateTime**: Parsed from strings or numeric timestamps
- **Guid**: Parsed from strings
- **Enums**: Parsed from strings or integers
- **Nullable types**: All of the above with `?` suffix
- **SQLite booleans**: Converts `0`/`1` to `false`/`true`

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime? LastUpdated { get; set; }  // Nullable
    public ProductStatus Status { get; set; }   // Enum
    public bool IsActive { get; set; }          // SQLite stores as 0/1
}

public enum ProductStatus
{
    Draft,
    Published,
    Archived
}
```

### Column Name Mapping

The default mapper automatically converts snake_case column names to PascalCase properties:

- `user_id` → `UserId`
- `email_address` → `EmailAddress`
- `created_at` → `CreatedAt`
- `is_active` → `IsActive`

```csharp
// Database columns: user_id, full_name, email_address, created_at
public class User
{
    public int UserId { get; set; }        // Maps from user_id
    public string FullName { get; set; }   // Maps from full_name
    public string EmailAddress { get; set; } // Maps from email_address
    public DateTime CreatedAt { get; set; }  // Maps from created_at
}
```

## Performance

- **Reflection caching**: Property info is cached using `ConcurrentDictionary`
- **Mapping cache**: Column-to-property mappings are cached per type
- **Minimal overhead**: <1ms for 1000 rows on typical hardware
- **Lazy execution**: Queries are only executed when you call an execution method

### Performance Tips

1. **Reuse mappers**: Create one mapper instance and reuse it
2. **Use `Take()` for limits**: Reduces data transfer and processing
3. **Project only needed columns**: `SELECT id, name` instead of `SELECT *`
4. **Use `CountAsync()` for counts**: More efficient than `.ToListAsync().Count()`
5. **Use `AnyAsync()` for existence checks**: More efficient than checking count

## Examples

### Pagination with Total Count

```csharp
var query = client.Query<Product>("products")
    .Where("category = ?", "Electronics");

var total = await query.CountAsync();
var page1 = await query.Skip(0).Take(20).ToListAsync();

Console.WriteLine($"Showing {page1.Count()} of {total} products");
```

### Search with Multiple Filters

```csharp
var searchTerm = "%laptop%";
var minPrice = 500m;
var maxPrice = 2000m;

var results = await client.Query<Product>("products")
    .Where("name LIKE ?", searchTerm)
    .Where("price >= ?", minPrice)
    .Where("price <= ?", maxPrice)
    .Where("is_active = ?", true)
    .OrderBy("price")
    .ToListAsync();
```

### Complex Joins (using direct SQL)

```csharp
var ordersWithCustomers = await client.QueryAsync<OrderWithCustomer>(@"
    SELECT 
        o.id as order_id,
        o.total as order_total,
        c.id as customer_id,
        c.name as customer_name
    FROM orders o
    INNER JOIN customers c ON o.customer_id = c.id
    WHERE o.created_at > @since
    ORDER BY o.created_at DESC
    ",
    new { since = DateTime.UtcNow.AddDays(-30) }
);
```

## What's New

### v1.2.1 - Computed Properties in Select()
- ✅ **Computed properties** - Use expressions in projections: `.Select(u => new { u.Name, IsAdult = u.Age >= 18 })`
- ✅ **Math operations** - Calculate values: `Total = u.Price * u.Quantity`, `Discount = u.Price * 0.1m`
- ✅ **Boolean expressions** - Create flags: `IsExpensive = u.Price > 100`, `IsMinor = u.Age < 18`
- ✅ **String methods** - Transform text: `UpperName = u.Name.ToUpper()`

### v1.2.0 - Select() Projection
- ✅ **Select() projection** - Select specific columns: `.Select(u => new { u.Id, u.Name })`
- ✅ **DTO mapping** - Project to strongly-typed DTOs
- ✅ **Performance optimization** - Reduce data transfer by selecting only needed columns

### v1.1.0 - Expression Tree LINQ
- ✅ **Expression tree support** - Type-safe queries: `.Where(u => u.Age >= 18)`
- ✅ **Lambda expressions** - Full IntelliSense and compile-time checking
- ✅ **OrderBy expressions** - `.OrderBy(u => u.Name)`, `.ThenBy(u => u.CreatedAt)`

## Coming Soon

- 🚧 **Include() for joins** - Automatic join and nested object mapping
- 🚧 **Having() clause** - Filter grouped results after aggregation
- 🚧 **IQueryable<T>** - Full deferred execution support

## Related Packages

- **CloudflareD1.NET** - Core D1 client ([NuGet](https://www.nuget.org/packages/CloudflareD1.NET))
- **CloudflareD1.NET.Migrations** - Schema migrations (coming soon)
- **CloudflareD1.NET.Testing** - Testing helpers (coming soon)

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET.Linq)
- 🐛 [Issue Tracker](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 📖 [Documentation](https://github.com/jdtoon/CloudflareD1.NET)
