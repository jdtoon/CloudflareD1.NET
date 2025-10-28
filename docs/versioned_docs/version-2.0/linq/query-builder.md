---
sidebar_position: 3
---

# Query Builder

The Query Builder provides a fluent, chainable API for constructing SQL queries with type safety and automatic parameter handling. Starting with v1.1.0, it supports both string-based queries and expression tree LINQ for compile-time type checking.

## Getting Started

Create a query builder by calling `Query<T>()` on your D1Client:

```csharp
var query = client.Query<User>("users");
```

## Two Query Syntaxes

The Query Builder supports two approaches:

1. **Expression-based (v1.1.0+)** - Type-safe lambda expressions with IntelliSense
2. **String-based** - Raw SQL fragments with parameter placeholders

Both approaches generate identical SQL and have the same performance. Choose based on your preference:

- Use **expressions** for compile-time safety and refactoring support
- Use **strings** for complex SQL or database-specific features

## Filtering with Where

Add WHERE clauses using either lambda expressions or parameterized SQL strings:

## Filtering with Where

Add WHERE clauses using either lambda expressions or parameterized SQL strings:

### Expression-Based Filtering (v1.1.0+)

Use lambda expressions for type-safe, compile-time checked queries:

```csharp
// Simple comparison
var adults = await client.Query<User>("users")
    .Where(u => u.Age >= 18)
    .ToListAsync();

// Multiple conditions with AND
var results = await client.Query<User>("users")
    .Where(u => u.Age >= 18 && u.Country == "US" && u.IsActive)
    .ToListAsync();

// OR conditions
var results = await client.Query<User>("users")
    .Where(u => u.Age < 18 || u.Age > 65)
    .ToListAsync();

// Null checks (generates IS NULL / IS NOT NULL)
var results = await client.Query<User>("users")
    .Where(u => u.Email != null)
    .ToListAsync();

// String methods
var results = await client.Query<User>("users")
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

// Captured variables
int minAge = 21;
var results = await client.Query<User>("users")
    .Where(u => u.Age >= minAge)
    .ToListAsync();
```

**Supported Expression Features:**
- **Comparison operators**: `>`, `<`, `>=`, `<=`, `==`, `!=`
- **Logical operators**: `&&` (AND), `||` (OR), `!` (NOT)
- **Null checks**: `!= null` (IS NOT NULL), `== null` (IS NULL)
- **String methods**: `Contains()`, `StartsWith()`, `EndsWith()`, `ToLower()`, `ToUpper()`
- **Math operators**: `+`, `-`, `*`, `/`
- **Captured variables**: Values from closure scope

### Basic Filtering

```csharp
// Single condition
var adults = await client.Query<User>("users")
    .Where("age >= ?", 18)
    .ToListAsync();

// Multiple conditions (combined with AND)
var results = await client.Query<User>("users")
    .Where("age >= ?", 18)
    .Where("country = ?", "US")
    .Where("is_active = ?", true)
    .ToListAsync();
```

### Comparison Operators

```csharp
// Equals
.Where("status = ?", "active")

// Not equals
.Where("status != ?", "deleted")

// Greater than / Less than
.Where("age > ?", 21)
.Where("price < ?", 100.00m)

// Greater/Less than or equal
.Where("age >= ?", 18)
.Where("quantity <= ?", 10)
```

### LIKE Queries

```csharp
// Contains
.Where("email LIKE ?", "%@example.com")

// Starts with
.Where("name LIKE ?", "John%")

// Ends with
.Where("filename LIKE ?", "%.pdf")
```

### Complex Conditions

```csharp
// Combine multiple conditions in one WHERE clause
.Where("(age > ? OR premium = ?) AND country = ?", 18, true, "US")

// NULL checks
.Where("deleted_at IS NULL")
.Where("email IS NOT NULL")
```

## Sorting with OrderBy

Sort results using lambda expressions or column names:

### Expression-Based Sorting (v1.1.0+)

```csharp
// Single property - ascending
var users = await client.Query<User>("users")
    .OrderBy(u => u.Name)
    .ToListAsync();

// Single property - descending
var users = await client.Query<User>("users")
    .OrderByDescending(u => u.CreatedAt)
    .ToListAsync();

// Multiple properties
var users = await client.Query<User>("users")
    .OrderBy(u => u.Country)
    .ThenBy(u => u.City)
    .ThenByDescending(u => u.CreatedAt)
    .ToListAsync();
```

### Single Column Sorting

```csharp
// Ascending (default)
var users = await client.Query<User>("users")
    .OrderBy("name")
    .ToListAsync();

// Descending
var users = await client.Query<User>("users")
    .OrderByDescending("created_at")
    .ToListAsync();
```

### Multi-Column Sorting

```csharp
var users = await client.Query<User>("users")
    .OrderBy("country")
    .ThenBy("city")
    .ThenByDescending("created_at")
    .ToListAsync();
```

## Pagination

### Take (LIMIT)

Limit the number of results:

```csharp
// Get first 10 results
var users = await client.Query<User>("users")
    .Take(10)
    .ToListAsync();
```

### Skip (OFFSET)

Skip a number of results:

```csharp
// Skip first 20 results
var users = await client.Query<User>("users")
    .Skip(20)
    .ToListAsync();
```

### Pagination Pattern

Combine Take and Skip for pagination:

```csharp
int pageNumber = 2; // Second page
int pageSize = 10;

var users = await client.Query<User>("users")
    .OrderBy("id")
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Execution Methods

### ToListAsync()

Execute the query and return all results:

```csharp
var users = await client.Query<User>("users")
    .Where("is_active = ?", true)
    .ToListAsync();
```

### FirstOrDefaultAsync()

Get the first result or null:

```csharp
var user = await client.Query<User>("users")
    .Where("email = ?", "john@example.com")
    .FirstOrDefaultAsync();

if (user != null)
{
    Console.WriteLine($"Found: {user.Name}");
}
```

### SingleAsync()

Get exactly one result (throws exception if 0 or >1):

```csharp
try
{
    var user = await client.Query<User>("users")
        .Where("id = ?", 123)
        .SingleAsync();
}
catch (InvalidOperationException ex)
{
    // Thrown if zero or multiple results
}
```

### SingleOrDefaultAsync()

Get exactly one result or null (throws if >1):

```csharp
var user = await client.Query<User>("users")
    .Where("email = ?", "unique@example.com")
    .SingleOrDefaultAsync();
```

## Aggregates

### CountAsync()

Get the count of matching records:

```csharp
var count = await client.Query<User>("users")
    .Where("is_active = ?", true)
    .CountAsync();

Console.WriteLine($"Active users: {count}");
```

### AnyAsync()

Check if any records match:

```csharp
var hasActiveUsers = await client.Query<User>("users")
    .Where("is_active = ?", true)
    .AnyAsync();

if (hasActiveUsers)
{
    Console.WriteLine("There are active users");
}
```

## Complete Examples

### Comparing Expression vs String Syntax

Both syntaxes produce identical SQL and performance:

```csharp
// Expression-based (type-safe, IntelliSense)
var expressionResults = await client.Query<Product>("products")
    .Where(p => p.IsActive && p.Price > 10.00m)
    .OrderBy(p => p.Name)
    .Take(10)
    .ToListAsync();

// String-based (flexible, supports any SQL)
var stringResults = await client.Query<Product>("products")
    .Where("is_active = ?", true)
    .Where("price > ?", 10.00m)
    .OrderBy("name")
    .Take(10)
    .ToListAsync();

// Both produce identical results!
```

### Search with Filters

Using expression-based syntax:

```csharp
public async Task<List<Product>> SearchProducts(
    string searchTerm,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize)
{
    var query = client.Query<Product>("products")
        .Where(p => p.IsActive);

    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where(p => p.Name.Contains(searchTerm));
    }

    if (minPrice.HasValue)
    {
        decimal min = minPrice.Value;
        query = query.Where(p => p.Price >= min);
    }

    if (maxPrice.HasValue)
    {
        decimal max = maxPrice.Value;
        query = query.Where(p => p.Price <= max);
    }

    return (await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync())
        .ToList();
}
```

Or using string-based syntax:

```csharp
public async Task<List<Product>> SearchProducts(
    string searchTerm,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize)
{
    var query = client.Query<Product>("products")
        .Where("is_active = ?", true);

    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where("name LIKE ?", $"%{searchTerm}%");
    }

    if (minPrice.HasValue)
    {
        query = query.Where("price >= ?", minPrice.Value);
    }

    if (maxPrice.HasValue)
    {
        query = query.Where("price <= ?", maxPrice.Value);
    }

    return (await query
        .OrderBy("name")
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync())
        .ToList();
}
```

### Paginated Results with Total Count

```csharp
public async Task<(List<User> Users, int TotalCount)> GetUsersPage(int page, int pageSize)
{
    var query = client.Query<User>("users")
        .Where("is_active = ?", true);

    var totalCount = await query.CountAsync();
    
    var users = (await query
        .OrderBy("created_at")
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync())
        .ToList();

    return (users, totalCount);
}
```

### Complex Business Logic

Using expression-based syntax with complex conditions:

```csharp
public async Task<List<Order>> GetRecentHighValueOrders()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-30);
    decimal minAmount = 1000.00m;
    
    return (await client.Query<Order>("orders")
        .Where(o => o.CreatedAt > cutoffDate 
                 && o.TotalAmount >= minAmount 
                 && o.Status != "cancelled")
        .OrderByDescending(o => o.TotalAmount)
        .ThenByDescending(o => o.CreatedAt)
        .Take(50)
        .ToListAsync())
        .ToList();
}
```

Or using string-based syntax:

```csharp
public async Task<List<Order>> GetRecentHighValueOrders()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-30);
    
    return (await client.Query<Order>("orders")
        .Where("created_at > ?", cutoffDate)
        .Where("total_amount >= ?", 1000.00m)
        .Where("status != ?", "cancelled")
        .OrderByDescending("total_amount")
        .ThenByDescending("created_at")
        .Take(50)
        .ToListAsync())
        .ToList();
}
```

## Method Chaining

All builder methods return the query builder itself, allowing you to chain methods:

```csharp
var results = await client.Query<User>("users")
    .Where("country = ?", "US")
    .Where("age >= ?", 18)
    .OrderBy("state")
    .ThenBy("city")
    .Skip(0)
    .Take(100)
    .ToListAsync();
```

## Performance Tips

1. **Use Take() for Limits**: Always limit results to avoid loading excessive data
2. **Count Without Loading**: Use `CountAsync()` instead of `.ToListAsync().Count()`
3. **Existence Checks**: Use `AnyAsync()` instead of checking if count > 0
4. **Index Your Columns**: Ensure WHERE and ORDER BY columns are indexed
5. **Avoid SELECT ***: Project only the columns you need (coming in future version)

## What's Next?

- **[Entity Mapping](entity-mapping)** - Learn how entities are mapped
