---
sidebar_position: 8
---

# Set Operations

Set operations allow you to combine results from multiple queries using SQL set operations: `UNION`, `UNION ALL`, `INTERSECT`, and `EXCEPT`.

## Overview

Set operations treat query results as mathematical sets, enabling powerful data combinations and filtering patterns.

**Available Operations:**
- **Union()** - Combine two queries, removing duplicates
- **UnionAll()** - Combine two queries, keeping all rows (faster)
- **Intersect()** - Return only rows in both queries
- **Except()** - Return rows from first query not in second

:::info Version
Set operations were introduced in **v1.8.0**.
:::

## Union - Combine with Deduplication

Combines results from two queries and removes duplicate rows.

```csharp
// Get young OR senior users (removes duplicates)
var youngUsers = client.Query<User>("users")
    .Where(u => u.Age < 30);

var seniorUsers = client.Query<User>("users")
    .Where(u => u.Age >= 60);

var result = await youngUsers.Union(seniorUsers).ToListAsync();
```

**Generated SQL:**
```sql
SELECT * FROM users WHERE Age < ? 
UNION 
SELECT * FROM users WHERE Age >= ?
```

## UnionAll - Combine with Duplicates

Combines results from two queries, keeping all rows including duplicates. This is **more performant** than `Union()` because it skips the deduplication step.

```csharp
// Combine all users from two different queries
var activeUsers = client.Query<User>("users")
    .Where(u => u.IsActive == true);

var premiumUsers = client.Query<User>("users")
    .Where(u => u.IsPremium == true);

var result = await activeUsers.UnionAll(premiumUsers).ToListAsync();
// Note: Users who are both active AND premium will appear twice
```

**Generated SQL:**
```sql
SELECT * FROM users WHERE IsActive = ? 
UNION ALL 
SELECT * FROM users WHERE IsPremium = ?
```

:::tip Performance
Use `UnionAll()` instead of `Union()` when you know there won't be duplicates or when duplicates are acceptable. It's significantly faster on large datasets.
:::

## Intersect - Common Elements

Returns only rows that appear in **both** queries. Useful for finding overlapping results.

```csharp
// Find users who are BOTH active AND premium
var activeUsers = client.Query<User>("users")
    .Where(u => u.IsActive == true);

var premiumUsers = client.Query<User>("users")
    .Where(u => u.IsPremium == true);

var activePremiumUsers = await activeUsers
    .Intersect(premiumUsers)
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT * FROM users WHERE IsActive = ? 
INTERSECT 
SELECT * FROM users WHERE IsPremium = ?
```

## Except - Set Difference

Returns rows from the first query that **don't appear** in the second query. Also known as "set difference" or "minus".

```csharp
// Find active users who are NOT premium
var allActiveUsers = client.Query<User>("users")
    .Where(u => u.IsActive == true);

var premiumUsers = client.Query<User>("users")
    .Where(u => u.IsPremium == true);

var activeFreeUsers = await allActiveUsers
    .Except(premiumUsers)
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT * FROM users WHERE IsActive = ? 
EXCEPT 
SELECT * FROM users WHERE IsPremium = ?
```

## Chaining Set Operations

You can chain multiple set operations together for complex queries:

```csharp
var young = client.Query<User>("users")
    .Where(u => u.Age < 25);

var middle = client.Query<User>("users")
    .Where(u => u.Age >= 25 && u.Age < 65);

var senior = client.Query<User>("users")
    .Where(u => u.Age >= 65);

// Get young OR senior users (exclude middle-aged)
var result = await young
    .Union(senior)
    .ToListAsync();

// Or chain multiple operations
var complexResult = await young
    .Union(senior)
    .Intersect(activeUsers)
    .ToListAsync();
```

## Execution Methods

Set operation results support standard execution methods:

### ToListAsync()
```csharp
var users = await youngUsers
    .Union(seniorUsers)
    .ToListAsync();
```

### CountAsync()
```csharp
var count = await youngUsers
    .Union(seniorUsers)
    .CountAsync();
```

### AnyAsync()
```csharp
var hasResults = await youngUsers
    .Union(seniorUsers)
    .AnyAsync();
```

### FirstOrDefaultAsync()
```csharp
var firstUser = await youngUsers
    .Union(seniorUsers)
    .FirstOrDefaultAsync();
```

## Complex Queries with Set Operations

### With ORDER BY and LIMIT

When queries have ORDER BY, LIMIT, or OFFSET, they're automatically wrapped as subqueries:

```csharp
// Top 5 youngest and top 5 oldest users
var youngest = client.Query<User>("users")
    .OrderBy(u => u.Age)
    .Take(5);

var oldest = client.Query<User>("users")
    .OrderByDescending(u => u.Age)
    .Take(5);

var result = await youngest.Union(oldest).ToListAsync();
```

**Generated SQL:**
```sql
(SELECT * FROM users ORDER BY Age ASC LIMIT 5)
UNION
(SELECT * FROM users ORDER BY Age DESC LIMIT 5)
```

### With WHERE Clauses

```csharp
// Active users from US or premium users from any country
var usActiveUsers = client.Query<User>("users")
    .Where(u => u.Country == "US" && u.IsActive);

var premiumUsers = client.Query<User>("users")
    .Where(u => u.IsPremium);

var result = await usActiveUsers
    .Union(premiumUsers)
    .ToListAsync();
```

## Best Practices

### 1. Use UnionAll for Performance
```csharp
// ✅ Good - Use UnionAll when duplicates are acceptable
var result = await query1.UnionAll(query2).ToListAsync();

// ❌ Slower - Union performs deduplication
var result = await query1.Union(query2).ToListAsync();
```

### 2. Schema Compatibility
All queries in a set operation must return the same column structure:

```csharp
// ✅ Good - Same entity type and structure
var query1 = client.Query<User>("users").Where(u => u.Age < 30);
var query2 = client.Query<User>("users").Where(u => u.Age >= 60);
var result = await query1.Union(query2).ToListAsync();

// ❌ Won't work - Different entity types
var users = client.Query<User>("users");
var orders = client.Query<Order>("orders");
// This will fail at runtime
var result = await users.Union(orders).ToListAsync();
```

### 3. Filter Before Set Operations
Apply filters in individual queries before combining:

```csharp
// ✅ Good - Filter first, then combine
var activeYoung = client.Query<User>("users")
    .Where(u => u.IsActive && u.Age < 30);
var activeSenior = client.Query<User>("users")
    .Where(u => u.IsActive && u.Age >= 60);
var result = await activeYoung.Union(activeSenior).ToListAsync();

// ❌ Less efficient - Combine first, then filter in memory
var young = client.Query<User>("users").Where(u => u.Age < 30);
var senior = client.Query<User>("users").Where(u => u.Age >= 60);
var allUsers = await young.Union(senior).ToListAsync();
var activeOnly = allUsers.Where(u => u.IsActive).ToList(); // In-memory filter
```

## Common Patterns

### Exclude Specific Records

```csharp
// Get all users except those in a specific list
var allUsers = client.Query<User>("users");
var excludedIds = new[] { 1, 2, 3 };
var excludedUsers = client.Query<User>("users")
    .Where(u => excludedIds.Contains(u.Id));

var result = await allUsers
    .Except(excludedUsers)
    .ToListAsync();
```

### Find Unique to Each Set

```csharp
// Users unique to set A (not in B)
var uniqueToA = await setA.Except(setB).ToListAsync();

// Users unique to set B (not in A)
var uniqueToB = await setB.Except(setA).ToListAsync();

// Symmetric difference (in A or B but not both)
var inANotB = setA.Except(setB);
var inBNotA = setB.Except(setA);
var symmetricDiff = await inANotB.Union(inBNotA).ToListAsync();
```

### Complex Multi-Set Operations

```csharp
// (A ∪ B) ∩ C - Union A and B, then intersect with C
var aUnionB = setA.Union(setB);
var result = await aUnionB.Intersect(setC).ToListAsync();

// A ∪ (B ∩ C) - Union A with the intersection of B and C
var bIntersectC = setB.Intersect(setC);
var result = await setA.Union(bIntersectC).ToListAsync();
```

## See Also

- [Query Builder](./query-builder.md) - Basic query operations
- [Existence Checks](./existence-checks.md) - AnyAsync() and AllAsync() with predicates
- [IQueryable](./iqueryable.md) - LINQ expression support
