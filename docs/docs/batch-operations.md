# Batch Operations

CloudflareD1.NET v1.11.0+ provides efficient batch operations for inserting, updating, and deleting multiple records in single database calls.

## Overview

Batch operations allow you to perform multiple similar operations efficiently by:

- Reducing network round-trips
- Improving performance for bulk data operations
- Executing multiple operations atomically
- Simplifying code for common bulk scenarios

## BatchInsertAsync

Insert multiple entities in a single batch operation.

### Basic Usage

```csharp
var products = new List<Product>
{
    new Product { Name = "Widget A", Price = 19.99, Stock = 100 },
    new Product { Name = "Widget B", Price = 29.99, Stock = 50 },
    new Product { Name = "Widget C", Price = 39.99, Stock = 75 }
};

var results = await client.BatchInsertAsync("products", products);
Console.WriteLine($"Inserted {results.Length} products");
```

### Features

- **Automatic property mapping** - C# properties are automatically mapped to snake_case database columns
- **AUTOINCREMENT support** - Default values (like `Id = 0`) are excluded automatically
- **Type-safe** - Uses generics for compile-time type checking
- **Batch execution** - All inserts executed in single database call

### Example: Bulk Import

```csharp
public async Task ImportUsersAsync(IEnumerable<UserImport> imports)
{
    var users = imports.Select(i => new User
    {
        Name = i.FullName,
        Email = i.EmailAddress,
        Age = i.Age,
        CreatedAt = DateTime.UtcNow.ToString("o")
    }).ToList();

    var results = await _client.BatchInsertAsync("users", users);
    
    Console.WriteLine($"Successfully imported {results.Length} users");
}
```

### Large Batches

BatchInsertAsync handles large datasets efficiently:

```csharp
// Insert 1000 products
var products = Enumerable.Range(1, 1000)
    .Select(i => new Product
    {
        Name = $"Product {i}",
        Price = i * 10.0,
        Stock = i * 5
    })
    .ToList();

var results = await client.BatchInsertAsync("products", products);
```

## BatchUpdateAsync

Update multiple entities based on a key selector.

### Basic Usage

```csharp
var productsToUpdate = await client.Query<Product>("products")
    .Where(p => p.Stock < 50)
    .ToListAsync();

// Update stock levels
foreach (var product in productsToUpdate)
{
    product.Stock += 100;
}

var results = await client.BatchUpdateAsync(
    "products",
    productsToUpdate,
    p => p.Id // Key selector
);
```

### Key Selector

The key selector specifies which property identifies each entity:

```csharp
// Update by Id (most common)
await client.BatchUpdateAsync("products", products, p => p.Id);

// Update by different key
await client.BatchUpdateAsync("products", products, p => p.Sku);
```

### Example: Bulk Price Update

```csharp
public async Task ApplyDiscountAsync(string category, double discountPercent)
{
    // Get all products in category
    var products = await _client.Query<Product>("products")
        .Where(p => p.Category == category)
        .ToListAsync();

    // Apply discount
    foreach (var product in products)
    {
        product.Price *= (1 - discountPercent / 100);
    }

    // Update all at once
    var results = await _client.BatchUpdateAsync("products", products, p => p.Id);
    
    Console.WriteLine($"Updated {results.Length} products with {discountPercent}% discount");
}
```

## BatchDeleteAsync

Delete multiple records by their IDs.

### Basic Usage

```csharp
var idsToDelete = new[] { 1, 2, 3, 4, 5 };

var results = await client.BatchDeleteAsync<int>("products", idsToDelete);
Console.WriteLine($"Deleted {results.Length} products");
```

### Custom Key Column

Specify a different key column name:

```csharp
var skusToDelete = new[] { "SKU-001", "SKU-002", "SKU-003" };

var results = await client.BatchDeleteAsync<string>(
    "products",
    skusToDelete,
    keyColumnName: "sku"
);
```

### Example: Bulk Cleanup

```csharp
public async Task CleanupOldOrdersAsync(DateTime cutoffDate)
{
    // Find old orders
    var oldOrders = await _client.QueryAsync(
        "SELECT id FROM orders WHERE created_at < @cutoff",
        new { cutoff = cutoffDate.ToString("o") });

    var idsToDelete = oldOrders.Results
        .Select(r => (int)r["id"]!)
        .ToList();

    if (idsToDelete.Any())
    {
        var results = await _client.BatchDeleteAsync<int>("orders", idsToDelete);
        Console.WriteLine($"Deleted {results.Length} old orders");
    }
}
```

## UpsertAsync

Insert a record or update it if it already exists (INSERT OR REPLACE).

### Basic Usage

```csharp
var product = new Product
{
    Id = 100,
    Name = "Special Widget",
    Price = 99.99,
    Stock = 10
};

// Will insert if Id=100 doesn't exist, update if it does
var result = await client.UpsertAsync("products", product);
```

### Example: Idempotent Operations

```csharp
public async Task SaveSettingAsync(string key, string value)
{
    var setting = new Setting
    {
        Key = key,
        Value = value,
        UpdatedAt = DateTime.UtcNow.ToString("o")
    };

    // Upsert ensures the setting is saved regardless of whether it exists
    await _client.UpsertAsync("settings", setting);
}
```

### Insert vs Update

```csharp
// First call: Inserts new record
await client.UpsertAsync("products", new Product { Id = 1, Name = "Widget", Price = 10 });

// Second call: Updates existing record
await client.UpsertAsync("products", new Product { Id = 1, Name = "Widget", Price = 15 });
```

## Complete Examples

### Import CSV Data

```csharp
public async Task ImportFromCsvAsync(string filePath)
{
    var records = new List<Product>();
    
    foreach (var line in File.ReadLines(filePath).Skip(1)) // Skip header
    {
        var parts = line.Split(',');
        records.Add(new Product
        {
            Name = parts[0],
            Price = double.Parse(parts[1]),
            Stock = int.Parse(parts[2])
        });
    }

    var results = await _client.BatchInsertAsync("products", records);
    Console.WriteLine($"Imported {results.Length} products from CSV");
}
```

### Sync External Data

```csharp
public async Task SyncProductsAsync(List<ExternalProduct> externalProducts)
{
    // Convert external format to internal
    var products = externalProducts.Select(ep => new Product
    {
        Id = ep.ExternalId,
        Name = ep.ProductName,
        Price = ep.UnitPrice,
        Stock = ep.QuantityAvailable
    }).ToList();

    // Upsert all products (insert new, update existing)
    foreach (var product in products)
    {
        await _client.UpsertAsync("products", product);
    }

    Console.WriteLine($"Synced {products.Count} products");
}
```

### Bulk Status Update

```csharp
public async Task ArchiveCompletedOrdersAsync()
{
    // Get completed orders
    var orders = await _client.Query<Order>("orders")
        .Where(o => o.Status == "completed" && o.CompletedDate < DateTime.UtcNow.AddDays(-30))
        .ToListAsync();

    // Update status
    foreach (var order in orders)
    {
        order.Status = "archived";
        order.ArchivedAt = DateTime.UtcNow.ToString("o");
    }

    // Batch update
    var results = await _client.BatchUpdateAsync("orders", orders, o => o.Id);
    Console.WriteLine($"Archived {results.Length} orders");
}
```

### Conditional Delete

```csharp
public async Task DeleteLowStockProductsAsync()
{
    // Find products with low stock
    var lowStockProducts = await _client.Query<Product>("products")
        .Where(p => p.Stock < 10 && p.Discontinued)
        .ToListAsync();

    var idsToDelete = lowStockProducts.Select(p => p.Id).ToList();

    if (idsToDelete.Any())
    {
        var results = await _client.BatchDeleteAsync<int>("products", idsToDelete);
        Console.WriteLine($"Deleted {results.Length} discontinued low-stock products");
    }
}
```

## Performance Comparison

### Without Batch Operations

```csharp
// ❌ Slow - N database calls
foreach (var product in products)
{
    await client.ExecuteAsync(
        "INSERT INTO products (name, price) VALUES (@name, @price)",
        new { name = product.Name, price = product.Price });
}
```

### With Batch Operations

```csharp
// ✅ Fast - 1 database call
await client.BatchInsertAsync("products", products);
```

**Performance gain:** 10-100x faster for batches of 10-1000 records.

## Batch Operations with Transactions

Combine batch operations with transactions for additional control:

```csharp
using var transaction = await client.BeginTransactionAsync();

try
{
    // Batch insert new products
    await client.BatchInsertAsync("products", newProducts);

    // Batch update existing products
    await client.BatchUpdateAsync("products", existingProducts, p => p.Id);

    // Batch delete discontinued products
    await client.BatchDeleteAsync<int>("products", discontinuedIds);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Best Practices

### 1. Use Appropriate Batch Sizes

Break very large operations into chunks:

```csharp
const int batchSize = 1000;
var allProducts = GetLargeProductList(); // e.g., 10,000 items

for (int i = 0; i < allProducts.Count; i += batchSize)
{
    var batch = allProducts.Skip(i).Take(batchSize).ToList();
    await client.BatchInsertAsync("products", batch);
}
```

### 2. Handle Partial Failures

```csharp
try
{
    var results = await client.BatchInsertAsync("products", products);
    
    // Check individual results if needed
    for (int i = 0; i < results.Length; i++)
    {
        if (!results[i].Success)
        {
            Console.WriteLine($"Failed to insert product {i}: {results[i].Error}");
        }
    }
}
catch (Exception ex)
{
    // Handle complete batch failure
    Console.WriteLine($"Batch operation failed: {ex.Message}");
}
```

### 3. Use Strongly-Typed Entities

```csharp
// ✅ Good - Type-safe
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
}

await client.BatchInsertAsync("products", products);

// ❌ Avoid - Error-prone
await client.BatchInsertAsync("products", anonymousObjects);
```

### 4. Property Naming Convention

C# properties automatically convert to snake_case:

```csharp
public class User
{
    public int Id { get; set; }           // → id
    public string FirstName { get; set; }  // → first_name
    public string Email { get; set; }      // → email
}
```

### 5. Validate Data Before Batch Operations

```csharp
// Validate before batch
var validProducts = products.Where(p => 
    !string.IsNullOrEmpty(p.Name) && 
    p.Price > 0 &&
    p.Stock >= 0
).ToList();

if (validProducts.Count < products.Count)
{
    Console.WriteLine($"Skipping {products.Count - validProducts.Count} invalid products");
}

await client.BatchInsertAsync("products", validProducts);
```

## API Reference

### BatchInsertAsync

```csharp
Task<D1QueryResult[]> BatchInsertAsync<T>(
    this ID1Client client,
    string tableName,
    IEnumerable<T> entities,
    CancellationToken cancellationToken = default
) where T : class
```

### BatchUpdateAsync

```csharp
Task<D1QueryResult[]> BatchUpdateAsync<T>(
    this ID1Client client,
    string tableName,
    IEnumerable<T> entities,
    Func<T, object> keySelector,
    CancellationToken cancellationToken = default
) where T : class
```

### BatchDeleteAsync

```csharp
Task<D1QueryResult[]> BatchDeleteAsync<TKey>(
    this ID1Client client,
    string tableName,
    IEnumerable<TKey> ids,
    string keyColumnName = "id",
    CancellationToken cancellationToken = default
)
```

### UpsertAsync

```csharp
Task<D1QueryResult> UpsertAsync<T>(
    this ID1Client client,
    string tableName,
    T entity,
    CancellationToken cancellationToken = default
) where T : class
```

## Limitations

### AUTOINCREMENT Columns

Properties with default values (like `Id = 0`) are automatically excluded from INSERT:

```csharp
var product = new Product
{
    Id = 0,  // Excluded from INSERT (default value)
    Name = "Widget",  // Included
    Price = 19.99     // Included
};
```

### Property Mapping

Only public, readable properties are mapped. Computed properties and methods are ignored:

```csharp
public class Product
{
    public int Id { get; set; }           // ✅ Mapped
    public string Name { get; set; }      // ✅ Mapped
    internal int Stock { get; set; }      // ❌ Not mapped (not public)
    public int Total => Price * Stock;    // ❌ Not mapped (computed)
}
```

### Table Names

Table names must match exactly (case-sensitive in SQLite):

```csharp
// ✅ Correct
await client.BatchInsertAsync("products", products);

// ❌ Wrong - table name mismatch
await client.BatchInsertAsync("Products", products); // Won't find "products" table
```

## See Also

- [Transactions](./transactions.md) - Atomic multi-operation support
- [LINQ Queries](./linq/overview.md) - Type-safe queries
- [Getting Started](./getting-started/overview.md) - Initial setup
