---
sidebar_position: 7
---

# GroupBy & Aggregations

The **GroupBy** feature allows you to group query results by one or more keys and perform aggregate calculations on each group. This is essential for analytics, reporting, and data summarization.

## Quick Start

```csharp
// Group users by age and count them
var usersByAge = await client.Query<User>("users")
    .GroupBy(u => u.Age)
    .Select(g => new AgeGroup
    {
        Age = g.Key,
        UserCount = g.Count()
    })
    .ToListAsync();
```

## Basic GroupBy

Group by a single property:

```csharp
public class AgeGroup
{
    public int Age { get; set; }
    public int UserCount { get; set; }
}

var groups = await client.Query<User>("users")
    .GroupBy(u => u.Age)
    .Select(g => new AgeGroup
    {
        Age = g.Key,        // The grouping key
        UserCount = g.Count()  // Count in this group
    })
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT age, COUNT(*) AS user_count
FROM users
GROUP BY age
```

## Aggregate Functions

### Count()

Count the number of items in each group:

```csharp
var categoryCounts = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryCount
    {
        Category = g.Key,
        Count = g.Count()
    })
    .ToListAsync();
```

### Sum()

Calculate the sum of values in each group:

```csharp
var categoryTotals = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryTotal
    {
        Category = g.Key,
        TotalPrice = g.Sum(p => p.Price)
    })
    .ToListAsync();
```

### Average()

Calculate the average value:

```csharp
var categoryAverages = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryAverage
    {
        Category = g.Key,
        AveragePrice = g.Average(p => p.Price)
    })
    .ToListAsync();
```

### Min() and Max()

Find minimum and maximum values:

```csharp
var categoryPriceRanges = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryPriceRange
    {
        Category = g.Key,
        MinPrice = g.Min(p => p.Price),
        MaxPrice = g.Max(p => p.Price)
    })
    .ToListAsync();
```

## Multiple Aggregates

Combine multiple aggregate functions in a single query:

```csharp
public class CategoryStats
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}

var stats = await client.Query<Product>("products")
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
```

**Generated SQL:**
```sql
SELECT category,
       COUNT(*) AS product_count,
       SUM(price) AS total_price,
       AVG(price) AS average_price,
       MIN(price) AS min_price,
       MAX(price) AS max_price
FROM products
GROUP BY category
```

## Complex Aggregate Expressions

Perform calculations inside aggregate functions:

```csharp
public class OrderTotal
{
    public int CustomerId { get; set; }
    public decimal TotalValue { get; set; }
}

// Calculate total order value (Price * Quantity)
var orderTotals = await client.Query<Order>("orders")
    .GroupBy(o => o.CustomerId)
    .Select(g => new OrderTotal
    {
        CustomerId = g.Key,
        TotalValue = g.Sum(o => o.Price * o.Quantity)
    })
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT customer_id, SUM(price * quantity) AS total_value
FROM orders
GROUP BY customer_id
```

**Supported Operations in Aggregates:**
- ✅ **Math**: `+`, `-`, `*`, `/`
- ✅ **Property access**: `p.Price`, `o.Quantity`
- ✅ **Constants**: `10`, `100.5`

## Combining with WHERE

Filter data before grouping:

```csharp
// Only count active users
var activeUsersByAge = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .GroupBy(u => u.Age)
    .Select(g => new AgeGroup
    {
        Age = g.Key,
        UserCount = g.Count()
    })
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT age, COUNT(*) AS user_count
FROM users
WHERE is_active = ?
GROUP BY age
```

:::tip
Use `.Where()` before `.GroupBy()` to filter rows before aggregation. This is more efficient than filtering after grouping.
:::

## Sorting Grouped Results

Use `OrderBy()` to sort groups:

```csharp
// Order by count descending
var topCategories = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryCount
    {
        Category = g.Key,
        Count = g.Count()
    })
    .OrderByDescending("count")
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT category, COUNT(*) AS count
FROM products
GROUP BY category
ORDER BY count DESC
```

## Pagination with GroupBy

Limit the number of groups returned:

```csharp
// Get top 10 categories by product count
var top10 = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryCount
    {
        Category = g.Key,
        Count = g.Count()
    })
    .OrderByDescending("count")
    .Take(10)
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT category, COUNT(*) AS count
FROM products
GROUP BY category
ORDER BY count DESC
LIMIT 10
```

## Complete Example

Here's a comprehensive example combining all features:

```csharp
public class ProductSalesReport
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}

// Generate sales report: active products, grouped by category, sorted by revenue
var salesReport = await client.Query<Product>("products")
    .Where(p => p.IsActive)
    .GroupBy(p => p.Category)
    .Select(g => new ProductSalesReport
    {
        Category = g.Key,
        ProductCount = g.Count(),
        TotalRevenue = g.Sum(p => p.Price * p.Quantity),
        AveragePrice = g.Average(p => p.Price),
        MinPrice = g.Min(p => p.Price),
        MaxPrice = g.Max(p => p.Price)
    })
    .OrderByDescending("total_revenue")
    .Take(20)
    .ToListAsync();

// Display results
foreach (var category in salesReport)
{
    Console.WriteLine($"{category.Category}:");
    Console.WriteLine($"  Products: {category.ProductCount}");
    Console.WriteLine($"  Revenue: ${category.TotalRevenue:N2}");
    Console.WriteLine($"  Avg Price: ${category.AveragePrice:N2}");
    Console.WriteLine($"  Price Range: ${category.MinPrice:N2} - ${category.MaxPrice:N2}");
}
```

## Type Requirements

Result classes must meet these requirements:

```csharp
// ✅ Good: Class with parameterless constructor
public class CategoryStats
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ✅ Good: Class with default values
public class AgeGroup
{
    public int Age { get; set; }
    public int UserCount { get; set; } = 0;
}

// ❌ Bad: No parameterless constructor
public class BadResult
{
    public BadResult(string name) { }
}
```

:::info Result Class Constraint
Result classes must have a parameterless constructor (`where TResult : class, new()`). This is required for entity mapping to work correctly.
:::

## API Reference

### GroupBy()

Groups query results by a key selector:

```csharp
IGroupByQueryBuilder<TSource, TKey> GroupBy<TKey>(
    Expression<Func<TSource, TKey>> keySelector
)
```

**Parameters:**
- `keySelector` - Expression to extract the grouping key

**Returns:** `IGroupByQueryBuilder<TSource, TKey>` for further operations

### Select()

Projects grouped results with aggregations:

```csharp
IGroupByProjectionQueryBuilder<TResult> Select<TResult>(
    Expression<Func<IGrouping<TKey, TSource>, TResult>> selector
) where TResult : class, new()
```

**Parameters:**
- `selector` - Expression to project each group to a result object

**Returns:** `IGroupByProjectionQueryBuilder<TResult>` for execution methods

### Aggregate Functions

Available on `IGrouping<TKey, TElement>`:

```csharp
int Count()
TValue Sum<TValue>(Expression<Func<TElement, TValue>> selector)
TValue Average<TValue>(Expression<Func<TElement, TValue>> selector)
TValue Min<TValue>(Expression<Func<TElement, TValue>> selector)
TValue Max<TValue>(Expression<Func<TElement, TValue>> selector)
```

## Performance Tips

1. **Filter before grouping**: Use `.Where()` before `.GroupBy()` to reduce data
2. **Limit results**: Use `.Take()` to limit the number of groups returned
3. **Index grouping keys**: Ensure database columns used in GROUP BY are indexed
4. **Select only needed aggregates**: Don't compute aggregates you won't use
5. **Use appropriate data types**: Match C# types to database column types

## Limitations

### Coming Soon
- **Having()** - Currently stubbed, will filter grouped results after aggregation
- **Composite keys** - Currently only single key GroupBy is supported
- **Nested groups** - Multiple levels of grouping not yet supported

### Current Constraints
- ✅ Single key GroupBy only
- ✅ Result classes must have parameterless constructor
- ✅ Aggregate expressions are limited to simple math operations

## Next Steps

- Learn about [Entity Mapping](entity-mapping.md) for custom result mapping
- Explore [Expression Trees](expression-trees.md) for advanced query building
- Check out [Query Builder](query-builder.md) for non-GroupBy operations

