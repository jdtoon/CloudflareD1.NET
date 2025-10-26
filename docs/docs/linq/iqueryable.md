---
sidebar_position: 5
---

# IQueryable&lt;T&gt; Support

CloudflareD1.NET.Linq v1.3.0 introduces full **IQueryable<T>** support with deferred execution, allowing you to use standard LINQ query syntax with composable, testable queries.

## Overview

IQueryable provides **deferred execution** - queries are built up incrementally and only execute when you enumerate the results or call a terminal operation like `ToListAsync()`.

```csharp
// Create IQueryable - NO query sent to database yet
IQueryable<User> queryable = client.AsQueryable<User>("users");

// Compose query - still not executed
var adults = queryable
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name);

// NOW the query executes
var results = await ((D1Queryable<User>)adults).ToListAsync();
```

## Getting Started

### Basic Usage

```csharp
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;

// Simple filtering
IQueryable<User> adults = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18);

var results = await ((D1Queryable<User>)adults).ToListAsync();
```

### Multiple Where Clauses

Multiple `Where()` calls are combined with AND:

```csharp
var youngAdults = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)  // First condition
    .Where(u => u.Age < 30);  // AND second condition

// Generates: WHERE age >= 18 AND age < 30
var results = await ((D1Queryable<User>)youngAdults).ToListAsync();
```

### Ordering

```csharp
// Order ascending
var byName = client.AsQueryable<User>("users")
    .OrderBy(u => u.Name);

// Order descending
var byAgeDesc = client.AsQueryable<User>("users")
    .OrderByDescending(u => u.Age);

// Multiple sort keys
var sorted = client.AsQueryable<User>("users")
    .OrderBy(u => u.LastName)
    .ThenBy(u => u.FirstName);
```

### Pagination

```csharp
// Skip first 20, take next 10
var page = client.AsQueryable<User>("users")
    .OrderBy(u => u.Id)
    .Skip(20)
    .Take(10);

var pageResults = await ((D1Queryable<User>)page).ToListAsync();
```

## Complex Queries

Combine multiple operations:

```csharp
var complexQuery = client.AsQueryable<User>("users")
    .Where(u => u.Age > 20)          // Filter
    .Where(u => u.IsActive)          // Additional filter (AND)
    .OrderBy(u => u.Name)            // Sort
    .Skip(10)                        // Pagination
    .Take(5);

var results = await ((D1Queryable<User>)complexQuery).ToListAsync();
// Generates: SELECT * FROM users WHERE age > 20 AND is_active = 1 
//            ORDER BY name LIMIT 5 OFFSET 10
```

## Terminal Operations

Terminal operations execute the query and return results:

### ToListAsync()

Execute query and return all results:

```csharp
var query = client.AsQueryable<User>("users")
    .Where(u => u.IsActive);

var users = await ((D1Queryable<User>)query).ToListAsync();
// Returns: List<User>
```

### CountAsync()

Count matching records:

```csharp
var countQuery = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18);

int adultCount = await ((D1Queryable<User>)countQuery).CountAsync();
// Generates: SELECT COUNT(*) as count FROM users WHERE age >= 18
```

### FirstOrDefaultAsync()

Get first matching record or null:

```csharp
var firstQuery = client.AsQueryable<User>("users")
    .Where(u => u.Email == "john@example.com")
    .OrderBy(u => u.CreatedAt);

var user = await ((D1Queryable<User>)firstQuery).FirstOrDefaultAsync();
// Returns: User or null
```

### SingleAsync() / SingleOrDefaultAsync()

Get exactly one matching record:

```csharp
var singleQuery = client.AsQueryable<User>("users")
    .Where(u => u.Id == 123);

var user = await ((D1Queryable<User>)singleQuery).SingleOrDefaultAsync();
// Returns: User or null
// Throws if multiple records match
```

### AnyAsync()

Check if any records match:

```csharp
var anyQuery = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 65);

bool hasSeniors = await ((D1Queryable<User>)anyQuery).AnyAsync();
// Returns: true or false
```

## Supported LINQ Methods

| Method | Description | Example |
|--------|-------------|---------|
| `Where()` | Filter records | `.Where(u => u.Age >= 18)` |
| `OrderBy()` | Sort ascending | `.OrderBy(u => u.Name)` |
| `OrderByDescending()` | Sort descending | `.OrderByDescending(u => u.Age)` |
| `ThenBy()` | Secondary sort ascending | `.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)` |
| `ThenByDescending()` | Secondary sort descending | `.OrderBy(u => u.Name).ThenByDescending(u => u.Age)` |
| `Take()` | Limit results | `.Take(10)` |
| `Skip()` | Skip records (offset) | `.Skip(20)` |
| `Select()` | Project to DTO/custom type | `.Select(u => new UserDTO { Id = u.Id })` |
| `ToListAsync()` | Execute and return all | `await query.ToListAsync()` |
| `CountAsync()` | Count matching records | `await query.CountAsync()` |
| `FirstOrDefaultAsync()` | Get first or null | `await query.FirstOrDefaultAsync()` |
| `SingleAsync()` | Get exactly one | `await query.SingleAsync()` |
| `SingleOrDefaultAsync()` | Get one or null | `await query.SingleOrDefaultAsync()` |
| `AnyAsync()` | Check if any match | `await query.AnyAsync()` |

## Deferred Execution Benefits

### Composability

Build queries incrementally:

```csharp
IQueryable<User> baseQuery = client.AsQueryable<User>("users");

// Add filtering based on conditions
if (minAge.HasValue)
    baseQuery = baseQuery.Where(u => u.Age >= minAge.Value);

if (mustBeActive)
    baseQuery = baseQuery.Where(u => u.IsActive);

// Add sorting
baseQuery = baseQuery.OrderBy(u => u.Name);

// Execute once
var results = await ((D1Queryable<User>)baseQuery).ToListAsync();
```

### Reusability

Create reusable query fragments:

```csharp
public IQueryable<User> GetActiveUsers(ID1Client client)
{
    return client.AsQueryable<User>("users")
        .Where(u => u.IsActive);
}

public IQueryable<User> GetAdults(IQueryable<User> query)
{
    return query.Where(u => u.Age >= 18);
}

// Compose reusable queries
var activeAdults = GetAdults(GetActiveUsers(client));
var results = await ((D1Queryable<User>)activeAdults).ToListAsync();
```

### Testability

Easy to unit test query logic:

```csharp
[Fact]
public void BuildsCorrectAdultQuery()
{
    // Arrange
    var mockClient = new Mock<ID1Client>();
    
    // Act - compose query without executing
    var query = mockClient.Object.AsQueryable<User>("users")
        .Where(u => u.Age >= 18)
        .OrderBy(u => u.Name);
    
    // Assert - verify query structure
    Assert.NotNull(query);
    Assert.IsType<D1Queryable<User>>(query);
}
```

## IQueryable vs Query&lt;T&gt;

CloudflareD1.NET.Linq offers two approaches:

### IQueryable&lt;T&gt; - Deferred Execution

```csharp
// Deferred - query doesn't execute until enumerated
var query = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18);
// NO database call yet

var results = await ((D1Queryable<User>)query).ToListAsync();
// NOW it executes
```

**Use when:**
- You need deferred execution
- Building queries dynamically
- Creating reusable query fragments
- Unit testing query composition

### Query&lt;T&gt; - Immediate Builder

```csharp
// Immediate builder - executes when you call ToListAsync
var results = await client.Query<User>("users")
    .Where(u => u.Age >= 18)
    .ToListAsync();
// Executes immediately
```

**Use when:**
- Simple, straightforward queries
- You want immediate execution control
- Less casting needed (no D1Queryable cast)

Both approaches are fully supported and can be used interchangeably based on your needs.

## Expression Translation

D1QueryProvider translates LINQ expressions to SQL:

```csharp
// LINQ Expression
var query = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Skip(10)
    .Take(5);

// Translates to SQL
SELECT * FROM users 
WHERE age >= ? AND is_active = ? 
ORDER BY name 
LIMIT 5 OFFSET 10
```

The provider handles:
- Lambda expression parsing
- Property name to column name mapping (PascalCase → snake_case)
- Parameter extraction and binding
- SQL generation with proper escaping

## Best Practices

### 1. Cast Once, Execute Once

```csharp
// Good - compose first, cast once
var query = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .Take(10);
    
var results = await ((D1Queryable<User>)query).ToListAsync();

// Avoid - multiple casts
var count = await ((D1Queryable<User>)query).CountAsync();  // Don't do this
var first = await ((D1Queryable<User>)query).FirstOrDefaultAsync();  // Or this
```

### 2. Use Specific Types

```csharp
// Good - specific return type
public async Task<List<User>> GetActiveUsersAsync()
{
    var query = client.AsQueryable<User>("users")
        .Where(u => u.IsActive);
    return await ((D1Queryable<User>)query).ToListAsync();
}

// Avoid - returning IQueryable from public methods
public IQueryable<User> GetActiveUsers()  // Leaks implementation
{
    return client.AsQueryable<User>("users").Where(u => u.IsActive);
}
```

### 3. Combine Filters Efficiently

```csharp
// Good - all filters in Where clauses
var users = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)
    .Where(u => u.IsActive)
    .Where(u => u.EmailVerified);

// Generates: WHERE age >= 18 AND is_active = 1 AND email_verified = 1
```

## Select() Projections

**New in v1.4.0**: IQueryable now supports `Select()` for projecting data into DTOs or custom types.

### Simple Projection

Project to a DTO with selected properties:

```csharp
// Define a DTO
public class UserSummary
{
    public long Id { get; set; }
    public string Name { get; set; }
}

// Simple projection
var summaries = client.AsQueryable<User>("users")
    .Select(u => new UserSummary 
    { 
        Id = u.Id, 
        Name = u.Name 
    });

var results = await ((D1ProjectionQueryable<UserSummary>)summaries).ToListAsync();
// Returns: List<UserSummary> with only Id and Name populated
```

### Computed Properties

Add calculated fields in your projections:

```csharp
public class UserWithAge
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsAdult { get; set; }  // Computed
}

var withComputed = client.AsQueryable<User>("users")
    .Select(u => new UserWithAge
    {
        Id = u.Id,
        Name = u.Name,
        Age = u.Age,
        IsAdult = u.Age >= 18  // Computed property
    });

var results = await ((D1ProjectionQueryable<UserWithAge>)withComputed).ToListAsync();
```

### Chaining with Other Operations

Combine Select() with Where, OrderBy, and pagination:

```csharp
// Filter, sort, then project
var adultSummaries = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 21)
    .OrderBy(u => u.Name)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });

var results = await ((D1ProjectionQueryable<UserSummary>)adultSummaries).ToListAsync();

// With pagination
var pagedResults = client.AsQueryable<User>("users")
    .OrderBy(u => u.Id)
    .Skip(20)
    .Take(10)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });

var page = await ((D1ProjectionQueryable<UserSummary>)pagedResults).ToListAsync();
```

### Complex Projections

Combine multiple computed properties:

```csharp
public class UserAnalysis
{
    public string Name { get; set; }
    public int Age { get; set; }
    public int YearsUntilRetirement { get; set; }
    public bool IsMinor { get; set; }
    public bool IsSenior { get; set; }
}

var analysis = client.AsQueryable<User>("users")
    .Select(u => new UserAnalysis
    {
        Name = u.Name,
        Age = u.Age,
        YearsUntilRetirement = 65 - u.Age,  // Math operation
        IsMinor = u.Age < 18,               // Boolean expression
        IsSenior = u.Age >= 65              // Another boolean
    });

var results = await ((D1ProjectionQueryable<UserAnalysis>)analysis).ToListAsync();
```

### Terminal Operations with Select()

All terminal operations work with projections:

```csharp
var projection = client.AsQueryable<User>("users")
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });

// Get first projected result
var first = await ((D1ProjectionQueryable<UserSummary>)projection)
    .FirstOrDefaultAsync();

// Count projected results
int count = await ((D1ProjectionQueryable<UserSummary>)projection)
    .CountAsync();

// Check if any match
bool any = await ((D1ProjectionQueryable<UserSummary>)projection)
    .AnyAsync();
```

### Select() Best Practices

**1. Select() must be the last LINQ operation:**

```csharp
// ✅ Correct - Select is last
var good = client.AsQueryable<User>("users")
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name });

// ❌ Wrong - can't chain after Select
var bad = client.AsQueryable<User>("users")
    .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
    .Where(s => s.Id > 10);  // This won't work
```

**2. Only use properties from the source type:**

```csharp
// ✅ Correct - uses User properties
.Select(u => new UserSummary { Id = u.Id, Name = u.Name })

// ❌ Wrong - can't access external variables
var minAge = 18;
.Select(u => new UserSummary { Id = u.Id, IsAdult = u.Age >= minAge })
```

**3. Keep computed properties simple:**

```csharp
// ✅ Good - simple calculations
.Select(u => new UserWithAge 
{ 
    Age = u.Age,
    IsAdult = u.Age >= 18,
    YearsUntil65 = 65 - u.Age 
})

// ⚠️ Complex logic - consider post-processing
.Select(u => new UserComplex
{
    // Very complex nested logic might be better done after fetching
})
```

## Limitations

Current IQueryable implementation has some limitations:

1. **Select() must be last** - Can't chain operations after Select()
2. **GroupBy() not supported** - Use raw SQL for aggregations
3. **Join() not supported** - Use raw SQL for joins
4. **Complex boolean logic** - Only AND between Where clauses (no OR yet)

For advanced scenarios not yet supported by IQueryable, use the `Query<T>()` builder or raw SQL queries.

## See Also

- [Query Builder](./query-builder.md) - Alternative Query<T>() approach
- [Expression Trees](./expression-trees.md) - How lambda expressions work
- [Entity Mapping](./entity-mapping.md) - Automatic object mapping
