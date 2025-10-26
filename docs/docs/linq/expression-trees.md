---
sidebar_position: 4
---

# Expression Tree LINQ

Starting with v1.1.0, CloudflareD1.NET.Linq supports expression tree-based queries using lambda expressions. This provides compile-time type checking, IntelliSense support, and refactoring safety while maintaining the same performance as string-based queries.

## Why Use Expression Trees?

**Benefits:**
- ✅ **Compile-time safety** - Catch typos and type errors before runtime
- ✅ **IntelliSense** - Full autocomplete for properties and methods
- ✅ **Refactoring support** - Rename properties across your codebase
- ✅ **Type checking** - Ensure correct types in comparisons
- ✅ **No SQL injection** - Parameters automatically handled

**When to Use:**
- Use **expressions** for most queries - safer and easier to maintain
- Use **strings** when you need complex SQL or database-specific features

## Basic Syntax

### Where Clauses

```csharp
// Simple comparison
var adults = await client.Query<User>("users")
    .Where(u => u.Age >= 18)
    .ToListAsync();

// Multiple conditions with AND
var activeAdults = await client.Query<User>("users")
    .Where(u => u.Age >= 18 && u.IsActive)
    .ToListAsync();

// Chaining multiple Where calls (also AND)
var results = await client.Query<User>("users")
    .Where(u => u.Age >= 18)
    .Where(u => u.Country == "US")
    .Where(u => u.IsActive)
    .ToListAsync();

// OR conditions
var minorsOrSeniors = await client.Query<User>("users")
    .Where(u => u.Age < 18 || u.Age > 65)
    .ToListAsync();

// Complex logic with parentheses
var results = await client.Query<User>("users")
    .Where(u => (u.Age > 18 || u.HasParentalConsent) && u.IsActive)
    .ToListAsync();
```

### OrderBy Clauses

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
    .ThenByDescending(u => u.Name)
    .ToListAsync();
```

## Supported Operators

### Comparison Operators

All standard comparison operators are supported:

```csharp
// Equals
.Where(u => u.Status == "active")

// Not equals
.Where(u => u.Status != "deleted")

// Greater than
.Where(u => u.Age > 21)

// Greater than or equal
.Where(u => u.Age >= 18)

// Less than
.Where(u => u.Price < 100.00m)

// Less than or equal
.Where(u => u.Quantity <= 10)
```

### Logical Operators

```csharp
// AND (&&)
.Where(u => u.Age >= 18 && u.Age < 65)

// OR (||)
.Where(u => u.IsAdmin || u.IsModerator)

// NOT (!)
.Where(u => !u.IsDeleted)

// Complex combinations
.Where(u => (u.Age >= 18 || u.HasParentalConsent) && !u.IsBanned)
```

### Null Checks

Null comparisons are automatically converted to SQL `IS NULL` / `IS NOT NULL`:

```csharp
// Check for null
.Where(u => u.Email == null)
// Generates: WHERE email IS NULL

// Check for not null
.Where(u => u.Email != null)
// Generates: WHERE email IS NOT NULL

// Combine with other conditions
.Where(u => u.Email != null && u.Email.Contains("@example.com"))
```

## String Methods

### Contains, StartsWith, EndsWith

```csharp
// Contains (generates LIKE '%value%')
var users = await client.Query<User>("users")
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

// StartsWith (generates LIKE 'value%')
var users = await client.Query<User>("users")
    .Where(u => u.Name.StartsWith("John"))
    .ToListAsync();

// EndsWith (generates LIKE '%value')
var users = await client.Query<User>("users")
    .Where(u => u.Filename.EndsWith(".pdf"))
    .ToListAsync();
```

### Case Conversion

```csharp
// ToLower (generates LOWER(column))
var users = await client.Query<User>("users")
    .Where(u => u.Email.ToLower() == "admin@example.com")
    .ToListAsync();

// ToUpper (generates UPPER(column))
var users = await client.Query<User>("users")
    .Where(u => u.Status.ToUpper() == "ACTIVE")
    .ToListAsync();
```

## Working with Variables

You can capture variables from your closure scope:

```csharp
// Simple variable
int minAge = 21;
var adults = await client.Query<User>("users")
    .Where(u => u.Age >= minAge)
    .ToListAsync();

// From method parameters
public async Task<List<User>> GetUsersByCountry(string country)
{
    return (await client.Query<User>("users")
        .Where(u => u.Country == country)
        .ToListAsync())
        .ToList();
}

// Complex expressions with variables
decimal minPrice = 10.00m;
decimal maxPrice = 100.00m;
var products = await client.Query<Product>("products")
    .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
    .ToListAsync();
```

## Math Operations

Basic math operators are supported:

```csharp
// Addition
.Where(u => u.Age + 5 > 30)

// Subtraction
.Where(p => p.Price - p.Discount > 50)

// Multiplication
.Where(i => i.Quantity * i.Price > 1000)

// Division
.Where(s => s.Total / s.Count < 10)
```

## Property Name Conversion

Properties are automatically converted to snake_case column names:

```csharp
public class User
{
    public int Id { get; set; }           // → id
    public string Name { get; set; }      // → name
    public string Email { get; set; }     // → email
    public bool IsActive { get; set; }    // → is_active
    public DateTime CreatedAt { get; set; } // → created_at
}

// Your C# code
.Where(u => u.IsActive)
// Generates: WHERE is_active = ?

.OrderBy(u => u.CreatedAt)
// Generates: ORDER BY created_at
```

## Combining Expression and String Syntax

You can mix both syntaxes in the same query:

```csharp
var results = await client.Query<User>("users")
    .Where(u => u.Age >= 18)              // Expression
    .Where("country = ?", "US")           // String
    .OrderBy(u => u.Name)                 // Expression
    .ThenByDescending("created_at")       // String
    .ToListAsync();
```

## Performance

Expression-based queries have the same performance as string-based queries:

- Expression parsing happens **once** when building the query
- Generated SQL is identical to hand-written SQL
- Parameters are properly bound (no SQL injection risk)
- No runtime overhead compared to string-based queries

## Complete Example

Here's a comprehensive example showing expression tree usage:

```csharp
public class UserService
{
    private readonly ID1Client _client;

    public UserService(ID1Client client)
    {
        _client = client;
    }

    public async Task<List<User>> SearchUsers(
        string searchTerm,
        int? minAge,
        int? maxAge,
        bool? isActive,
        string country,
        int page,
        int pageSize)
    {
        // Start with base query
        var query = _client.Query<User>("users");

        // Apply filters conditionally using expressions
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => 
                u.Name.Contains(searchTerm) || 
                u.Email.Contains(searchTerm));
        }

        if (minAge.HasValue)
        {
            int min = minAge.Value;
            query = query.Where(u => u.Age >= min);
        }

        if (maxAge.HasValue)
        {
            int max = maxAge.Value;
            query = query.Where(u => u.Age <= max);
        }

        if (isActive.HasValue)
        {
            bool active = isActive.Value;
            query = query.Where(u => u.IsActive == active);
        }

        if (!string.IsNullOrEmpty(country))
        {
            query = query.Where(u => u.Country == country);
        }

        // Execute with sorting and pagination
        return (await query
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .ToList();
    }

    public async Task<List<User>> GetRecentActiveUsers(int count)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        return (await _client.Query<User>("users")
            .Where(u => u.IsActive && 
                       u.LastLoginAt > cutoffDate && 
                       u.Email != null)
            .OrderByDescending(u => u.LastLoginAt)
            .Take(count)
            .ToListAsync())
            .ToList();
    }
}
```

## Limitations

Expression trees support most common LINQ patterns, but have some limitations:

**Not Supported:**
- `Select()` projection (coming in future version)
- `Join()` operations
- `GroupBy()` grouping
- Subqueries
- `IN` operator with expression lists (use string syntax)

**Workarounds:**
- For complex SQL, use string-based Where clauses
- Mix expression and string syntax in the same query
- Use raw SQL with `QueryAsync()` for advanced scenarios

## What's Next?

- Learn about [Entity Mapping](entity-mapping) to customize property mappings
- See [Query Builder](query-builder) for the complete API reference
