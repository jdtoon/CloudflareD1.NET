---
sidebar_position: 7
---

# Join Operations

Perform INNER JOIN and LEFT JOIN operations to combine data from multiple tables.

## Overview

CloudflareD1.NET.Linq supports joining multiple tables with type-safe key selectors and multi-table projections. Join operations allow you to combine related data from different tables in a single query.

## Join Types

- **INNER JOIN** (`Join()`) - Returns only rows where there's a match in both tables
- **LEFT JOIN** (`LeftJoin()`) - Returns all rows from the left table, with NULL for non-matching right rows

## Basic Usage

### INNER JOIN

Use `Join()` to combine rows from two tables where the join keys match:

```csharp
using CloudflareD1.NET.Linq;

var ordersWithCustomers = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),  // Inner table
        order => order.CustomerId,            // Outer key selector
        customer => customer.Id)              // Inner key selector
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        OrderTotal = order.Total,
        OrderDate = order.CreatedAt,
        CustomerName = customer.Name,
        CustomerEmail = customer.Email
    })
    .ToListAsync();
```

Generated SQL:
```sql
SELECT orders.id AS order_id, orders.total AS order_total, 
       orders.created_at AS order_date,
       customers.name AS customer_name, customers.email AS customer_email
FROM orders
INNER JOIN customers ON orders.customer_id = customers.id
```

### LEFT JOIN

Use `LeftJoin()` to include all rows from the left table, even if there's no match:

```csharp
var usersWithOrders = await client.Query<User>("users")
    .LeftJoin(
        client.Query<Order>("orders"),
        user => user.Id,
        order => order.UserId)
    .Select((user, order) => new UserWithOrder
    {
        UserId = user.Id,
        UserName = user.Name,
        OrderId = order.Id,           // Will be 0 for users without orders
        OrderTotal = order.Total      // Will be 0.0 for users without orders
    })
    .ToListAsync();
```

Generated SQL:
```sql
SELECT users.id AS user_id, users.name AS user_name,
       orders.id AS order_id, orders.total AS order_total
FROM users
LEFT JOIN orders ON users.id = orders.user_id
```

## Filtering Joined Results

### WHERE Clause

Filter joined results using `Where()`:

```csharp
// High-value orders only
var highValueOrders = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        OrderTotal = order.Total,
        CustomerName = customer.Name
    })
    .Where(result => result.OrderTotal > 1000)  // Filter after projection
    .ToListAsync();
```

Generated SQL:
```sql
SELECT orders.id AS order_id, orders.total AS order_total,
       customers.name AS customer_name
FROM orders
INNER JOIN customers ON orders.customer_id = customers.id
WHERE (order_total > ?)
```

### Pre-Join Filtering

Filter individual tables before joining:

```csharp
// Only active customers with recent orders
var recentActiveOrders = await client.Query<Order>("orders")
    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30))  // Filter orders
    .Join(
        client.Query<Customer>("customers")
            .Where(c => c.IsActive),                          // Filter customers
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        CustomerName = customer.Name
    })
    .ToListAsync();
```

## Sorting and Pagination

### ORDER BY

Sort joined results:

```csharp
var recentOrders = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        OrderDate = order.CreatedAt,
        CustomerName = customer.Name,
        OrderTotal = order.Total
    })
    .OrderByDescending("order_date")    // Sort by date, newest first
    .ToListAsync();
```

### LIMIT and OFFSET

Paginate joined results:

```csharp
// Top 20 highest-value orders
var topOrders = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        OrderTotal = order.Total,
        CustomerName = customer.Name
    })
    .OrderByDescending("order_total")
    .Take(20)
    .ToListAsync();

// Second page (skip 20, take 20)
var page2 = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        CustomerName = customer.Name
    })
    .Skip(20)
    .Take(20)
    .ToListAsync();
```

## Aggregation with Joins

### COUNT

Count joined results:

```csharp
// How many orders have customer information?
var orderCount = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new { order.Id, customer.Name })
    .CountAsync();

Console.WriteLine($"Found {orderCount} orders with customer data");
```

### FirstOrDefault

Get first matching result:

```csharp
var firstOrder = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        CustomerName = customer.Name
    })
    .OrderBy("order_id")
    .FirstOrDefaultAsync();
```

## Complete Examples

### Order History Report

```csharp
public class OrderReportService
{
    private readonly D1Client _client;

    public async Task<IEnumerable<OrderReport>> GetOrderReport(DateTime startDate)
    {
        return await _client.Query<Order>("orders")
            .Where(o => o.CreatedAt >= startDate)
            .Where(o => o.Status == "completed")
            .Join(
                _client.Query<Customer>("customers"),
                order => order.CustomerId,
                customer => customer.Id)
            .Select((order, customer) => new OrderReport
            {
                OrderId = order.Id,
                OrderDate = order.CreatedAt,
                OrderTotal = order.Total,
                CustomerName = customer.Name,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.Phone
            })
            .OrderByDescending("order_date")
            .ToListAsync();
    }
}

public class OrderReport
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal OrderTotal { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
}
```

### Customer Activity Dashboard

```csharp
public class CustomerDashboard
{
    private readonly D1Client _client;

    public async Task<CustomerStats?> GetCustomerStats(int customerId)
    {
        var customer = await _client.Query<Customer>("customers")
            .Where(c => c.Id == customerId)
            .FirstOrDefaultAsync();

        if (customer == null) return null;

        var orders = await _client.Query<Order>("orders")
            .Where(o => o.CustomerId == customerId)
            .Where(o => o.Status == "completed")
            .ToListAsync();

        return new CustomerStats
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            TotalOrders = orders.Count(),
            TotalSpent = orders.Sum(o => o.Total),
            AverageOrderValue = orders.Average(o => o.Total),
            LastOrderDate = orders.Max(o => o.CreatedAt)
        };
    }
}
```

### LEFT JOIN with NULL Handling

```csharp
public async Task<IEnumerable<UserActivity>> GetUserActivity()
{
    var results = await _client.Query<User>("users")
        .Where(u => u.IsActive)
        .LeftJoin(
            _client.Query<Order>("orders"),
            user => user.Id,
            order => order.UserId)
        .Select((user, order) => new UserActivity
        {
            UserId = user.Id,
            UserName = user.Name,
            UserEmail = user.Email,
            OrderId = order.Id,
            OrderTotal = order.Total,
            OrderDate = order.CreatedAt
        })
        .ToListAsync();

    // Group by user to handle multiple orders
    var userActivity = results
        .GroupBy(r => r.UserId)
        .Select(g => new UserSummary
        {
            UserId = g.Key,
            UserName = g.First().UserName,
            UserEmail = g.First().UserEmail,
            TotalOrders = g.Count(x => x.OrderId > 0),  // OrderId = 0 means no order
            HasOrders = g.Any(x => x.OrderId > 0)
        });

    return userActivity;
}
```

## Entity Models

Define your entity classes to match database tables:

```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; }
}

public class OrderWithCustomer
{
    public int OrderId { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
}
```

## Important Notes

:::info
**Column Aliasing**
Join operations automatically generate unique column aliases to avoid naming conflicts. For example, both `orders.id` and `customers.id` will be aliased as `order_id` and `customer_id` in the SELECT clause.
:::

:::tip
**NULL Handling in LEFT JOIN**
For LEFT JOIN, when there's no matching row in the right table:
- Numeric properties will be `0`
- String properties will be empty string `""`
- DateTime properties will be `DateTime.MinValue`
- Nullable properties will be `null`

Check for `order.Id > 0` or `!string.IsNullOrEmpty(order.Status)` to detect non-matching rows.
:::

:::warning
**Key Selector Limitations**
Join key selectors must be simple property access expressions:

```csharp
// ✅ GOOD: Simple property
.Join(..., order => order.CustomerId, customer => customer.Id)

// ❌ BAD: Complex expressions not supported
.Join(..., order => order.CustomerId * 2, customer => customer.Id + 1)
```

For complex join conditions, use raw SQL with `QueryAsync<T>()`.
:::

## Performance Tips

1. **Filter Before Joining**: Use `Where()` on individual tables before joining to reduce the number of rows
2. **Select Only Needed Columns**: Don't select all columns if you only need a few
3. **Use Indexes**: Ensure join keys have database indexes for better performance
4. **Limit Results**: Use `Take()` to limit large result sets

```csharp
// ✅ OPTIMIZED: Filter first, select only needed columns, limit results
var results = await client.Query<Order>("orders")
    .Where(o => o.Status == "completed")           // Filter early
    .Join(
        client.Query<Customer>("customers")
            .Where(c => c.IsActive),               // Filter early
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new               // Only needed columns
    {
        order.Id,
        order.Total,
        customer.Name
    })
    .Take(100)                                     // Limit results
    .ToListAsync();
```

## Supported Operations

| Operation | Description | Example |
|-----------|-------------|---------|
| `Join()` | INNER JOIN - matching rows only | `.Join(innerQuery, o => o.Key, i => i.Key)` |
| `LeftJoin()` | LEFT JOIN - all left rows | `.LeftJoin(innerQuery, o => o.Key, i => i.Key)` |
| `Select()` | Project to result type | `.Select((o, i) => new Result { ... })` |
| `Where()` | Filter joined results | `.Where(r => r.Total > 100)` |
| `OrderBy()` | Sort results | `.OrderBy("column_name")` |
| `OrderByDescending()` | Sort descending | `.OrderByDescending("column_name")` |
| `Take()` | Limit results | `.Take(10)` |
| `Skip()` | Skip rows | `.Skip(20)` |
| `CountAsync()` | Count results | `.CountAsync()` |
| `FirstOrDefaultAsync()` | Get first result | `.FirstOrDefaultAsync()` |

## Next Steps

- Learn about [Having Clause](./having.md) for filtering grouped results
- Explore [GroupBy](./groupby.md) for aggregations
- See [Expression Trees](./expression-trees.md) for complex queries
