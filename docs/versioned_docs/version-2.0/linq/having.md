---
sidebar_position: 6
---

# Having Clause

Filter grouped results after aggregation using the `Having()` clause.

## Overview

The `Having()` clause allows you to filter the results of a `GroupBy()` operation based on aggregate values. While `Where()` filters rows before grouping, `Having()` filters groups after aggregation.

**Use `Where()` when**: Filtering individual rows before grouping
**Use `Having()` when**: Filtering groups based on aggregate values (count, sum, average, etc.)

## Basic Usage

### Filter by Count

Filter groups based on the number of items:

```csharp
using CloudflareD1.NET.Linq;

// Groups with more than 5 users
var largeAgeGroups = await client.Query<User>("users")
    .GroupBy(u => u.Age)
    .Having(g => g.Count() > 5)
    .Select(g => new AgeGroup
    {
        Age = g.Key,
        UserCount = g.Count()
    })
    .ToListAsync();
```

Generated SQL:
```sql
SELECT age, COUNT(*) AS user_count
FROM users
GROUP BY age
HAVING COUNT(*) > 5
```

### Filter by Sum

Filter groups based on sum of values:

```csharp
// Categories with total sales over $10,000
var topCategories = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Having(g => g.Sum(p => p.Price) > 10000)
    .Select(g => new CategoryStats
    {
        Category = g.Key,
        TotalSales = g.Sum(p => p.Price),
        ProductCount = g.Count()
    })
    .ToListAsync();
```

Generated SQL:
```sql
SELECT category, SUM(price) AS total_sales, COUNT(*) AS product_count
FROM products
GROUP BY category
HAVING SUM(price) > 10000
```

## Aggregate Functions

### Average

Filter by average values:

```csharp
// Categories with average price >= $100
var expensiveCategories = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Having(g => g.Average(p => p.Price) >= 100)
    .Select(g => new CategoryStats
    {
        Category = g.Key,
        AveragePrice = g.Average(p => p.Price),
        ProductCount = g.Count()
    })
    .ToListAsync();
```

### Min and Max

Filter by minimum or maximum values:

```csharp
// Age groups where youngest person is over 18
var adultGroups = await client.Query<User>("users")
    .GroupBy(u => u.Country)
    .Having(g => g.Min(u => u.Age) > 18)
    .Select(g => new CountryStats
    {
        Country = g.Key,
        MinAge = g.Min(u => u.Age),
        MaxAge = g.Max(u => u.Age)
    })
    .ToListAsync();

// Groups where max price is under $1000
var affordableCategories = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Having(g => g.Max(p => p.Price) <= 1000)
    .Select(g => new { Category = g.Key, MaxPrice = g.Max(p => p.Price) })
    .ToListAsync();
```

## Comparison Operators

The `Having()` clause supports all standard comparison operators:

- `>` - Greater than
- `<` - Less than
- `>=` - Greater than or equal to
- `<=` - Less than or equal to
- `==` - Equal to
- `!=` - Not equal to

```csharp
// Different comparisons
var results1 = await query.Having(g => g.Count() > 10).ToListAsync();      // More than 10
var results2 = await query.Having(g => g.Count() < 5).ToListAsync();       // Less than 5
var results3 = await query.Having(g => g.Count() >= 10).ToListAsync();     // At least 10
var results4 = await query.Having(g => g.Count() <= 5).ToListAsync();      // At most 5
var results5 = await query.Having(g => g.Count() == 10).ToListAsync();     // Exactly 10
var results6 = await query.Having(g => g.Count() != 0).ToListAsync();      // Not zero
```

## Combining with Other Clauses

### WHERE + HAVING

Use `Where()` to filter rows before grouping, and `Having()` to filter groups:

```csharp
var filteredGroups = await client.Query<User>("users")
    .Where(u => u.IsActive)              // Filter: only active users
    .GroupBy(u => u.Country)             // Group by country
    .Having(g => g.Count() >= 10)        // Filter: groups with 10+ users
    .Select(g => new CountryStats
    {
        Country = g.Key,
        UserCount = g.Count(),
        AvgAge = g.Average(u => u.Age)
    })
    .ToListAsync();
```

Generated SQL:
```sql
SELECT country, COUNT(*) AS user_count, AVG(age) AS avg_age
FROM users
WHERE is_active = 1
GROUP BY country
HAVING COUNT(*) >= 10
```

### HAVING + ORDER BY

Combine `Having()` with sorting:

```csharp
var topCountries = await client.Query<User>("users")
    .GroupBy(u => u.Country)
    .Having(g => g.Count() >= 100)
    .Select(g => new { Country = g.Key, Count = g.Count() })
    .OrderByDescending("count")
    .ToListAsync();
```

### HAVING + LIMIT

Combine `Having()` with pagination:

```csharp
var top10 = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Having(g => g.Sum(p => p.Price) > 5000)
    .Select(g => new { Category = g.Key, Total = g.Sum(p => p.Price) })
    .OrderByDescending("total")
    .Take(10)
    .ToListAsync();
```

## Complete Example

A comprehensive example combining multiple features:

```csharp
public class OrderAnalyzer
{
    private readonly D1Client _client;

    public async Task<IEnumerable<CustomerSegment>> GetHighValueCustomers()
    {
        return await _client.Query<Order>("orders")
            .Where(o => o.Status == "completed")        // Only completed orders
            .Where(o => o.CreatedAt >= DateTime.UtcNow.AddYears(-1))  // Last year
            .GroupBy(o => o.CustomerId)                 // Group by customer
            .Having(g => g.Count() >= 5)                // At least 5 orders
            .Having(g => g.Sum(o => o.Total) > 1000)    // Total spent > $1000
            .Select(g => new CustomerSegment
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.Total),
                AverageOrderValue = g.Average(o => o.Total),
                LargestOrder = g.Max(o => o.Total)
            })
            .OrderByDescending("total_spent")           // Highest spenders first
            .Take(100)                                   // Top 100
            .ToListAsync();
    }
}

public class CustomerSegment
{
    public int CustomerId { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal LargestOrder { get; set; }
}
```

Generated SQL:
```sql
SELECT customer_id, COUNT(*) AS order_count, 
       SUM(total) AS total_spent, AVG(total) AS average_order_value,
       MAX(total) AS largest_order
FROM orders
WHERE status = ? AND created_at >= ?
GROUP BY customer_id
HAVING COUNT(*) >= 5 AND SUM(total) > 1000
ORDER BY total_spent DESC
LIMIT 100
```

## Important Notes

:::info
**HAVING vs WHERE**
- `Where()` filters rows **before** grouping (operates on individual rows)
- `Having()` filters groups **after** aggregation (operates on group results)
:::

:::tip
**Performance Consideration**
Use `Where()` to filter as many rows as possible before grouping. This reduces the amount of data that needs to be grouped and makes your query faster.

```csharp
// ✅ GOOD: Filter first, then group
await query
    .Where(u => u.IsActive)      // Reduces rows before grouping
    .GroupBy(u => u.Country)
    .Having(g => g.Count() > 10)
    .ToListAsync();

// ⚠️ LESS EFFICIENT: Group all rows, then filter
await query
    .GroupBy(u => u.Country)
    .Having(g => g.Count() > 10)  // Groups all rows including inactive
    .ToListAsync();
```
:::

## Supported Operations

| Aggregate | Description | Example |
|-----------|-------------|---------|
| `Count()` | Number of items in group | `g.Count() > 10` |
| `Sum(x => x.Property)` | Sum of values | `g.Sum(p => p.Price) > 1000` |
| `Average(x => x.Property)` | Average of values | `g.Average(u => u.Age) >= 25` |
| `Min(x => x.Property)` | Minimum value | `g.Min(p => p.Price) < 50` |
| `Max(x => x.Property)` | Maximum value | `g.Max(u => u.Age) <= 65` |

## Next Steps

- Learn about [Join Operations](./joins.md) for multi-table queries
- Explore [GroupBy](./groupby.md) for basic grouping operations
- See [IQueryable](./iqueryable.md) for deferred execution
