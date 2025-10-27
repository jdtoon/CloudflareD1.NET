# Query Optimization with CompiledQuery

*Available in v1.10.0+*

## Overview

`CompiledQuery` provides query optimization by pre-compiling LINQ expressions to SQL once and reusing them across multiple executions. This eliminates the overhead of repeated expression tree translation and significantly improves performance for queries that are executed multiple times.

## Key Benefits

- **🚀 Performance**: Compile queries once, execute many times
- **💾 Caching**: Automatic caching of compiled SQL and parameters
- **📊 Statistics**: Built-in cache hit/miss tracking
- **🔒 Thread-Safe**: Concurrent execution support
- **⚡ Zero Overhead**: After compilation, no expression tree processing

## Basic Usage

### Simple Query Compilation

```csharp
using CloudflareD1.NET.Linq.Query;

// Compile a query once
var compiledQuery = CompiledQuery.Create<User>(
    "users",
    q => q.Where(u => u.Age > 25)
);

// Execute multiple times - no recompilation!
var results1 = await compiledQuery.ExecuteAsync(client);
var results2 = await compiledQuery.ExecuteAsync(client);
var results3 = await compiledQuery.ExecuteAsync(client);
```

### With Projections

```csharp
var compiledQuery = CompiledQuery.Create<User, UserSummary>(
    "users",
    q => q.Where(u => u.IsActive)
          .OrderBy(u => u.Name)
          .Select(u => new UserSummary
          {
              Id = u.Id,
              Name = u.Name
          })
);

var summaries = await compiledQuery.ExecuteAsync(client);
```

## Advanced Features

### Complex Queries

CompiledQuery supports all LINQ query features:

```csharp
var compiledQuery = CompiledQuery.Create<User>(
    "users",
    q => q.Where(u => u.Age >= 21 && u.Age <= 65)
          .Where(u => u.IsActive)
          .OrderBy(u => u.LastName)
          .ThenBy(u => u.FirstName)
          .Skip(20)
          .Take(10)
);
```

### Pagination Pattern

```csharp
// Compile a reusable pagination query
var paginationQuery = CompiledQuery.Create<Product>(
    "products",
    q => q.Where(p => p.Category == "Electronics")
          .OrderBy(p => p.Price)
          .Skip(page * pageSize)
          .Take(pageSize)
);

// Execute for different pages
var page1 = await paginationQuery.ExecuteAsync(client);
var page2 = await paginationQuery.ExecuteAsync(client);
```

**Note**: Parameters like `page` and `pageSize` are captured from closures at compilation time. If you need different parameter values, create separate compiled queries.

### With Distinct

```csharp
var compiledQuery = CompiledQuery.Create<User>(
    "users",
    q => q.Where(u => u.Department == "Engineering")
          .Select(u => new { u.City, u.State })
          .Distinct()
);
```

## Caching Behavior

### Automatic Caching

CompiledQuery automatically caches compiled queries based on:
- Table name
- Entity type
- Generated SQL
- **Parameter values**

Two calls with identical queries will reuse the same compiled query:

```csharp
// First call - cache miss, compiles query
var query1 = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 30));

// Second call - cache hit, reuses compiled query
var query2 = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 30));
```

Different parameter values create separate cache entries:

```csharp
// These create separate cached entries
var query1 = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 25));
var query2 = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 30));
var query3 = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 35));
```

### Cache Statistics

Monitor cache performance:

```csharp
var stats = CompiledQuery.GetStatistics();
Console.WriteLine($"Cache Hits: {stats.CacheHits}");
Console.WriteLine($"Cache Misses: {stats.CacheMisses}");
Console.WriteLine($"Cache Size: {stats.CacheSize}");
Console.WriteLine($"Hit Ratio: {(double)stats.CacheHits / (stats.CacheHits + stats.CacheMisses):P}");
```

### Cache Management

Clear the cache when needed:

```csharp
// Clear all cached queries and reset statistics
CompiledQuery.ClearCache();
```

## Performance Guidelines

### When to Use CompiledQuery

✅ **Use CompiledQuery for:**
- Queries executed repeatedly (e.g., in loops, API endpoints)
- Hot path queries in performance-critical code
- Pagination queries
- Dashboard/reporting queries
- Real-time data fetching

❌ **Don't use CompiledQuery for:**
- One-time queries
- Dynamic queries with changing structure
- Ad-hoc queries
- Queries with many parameter variations

### Performance Comparison

| Scenario | Regular Query | CompiledQuery | Improvement |
|----------|--------------|---------------|-------------|
| First execution | 100ms | 100ms | 0% |
| 2nd execution | 100ms | 5ms | **95%** |
| 10th execution | 100ms | 5ms | **95%** |
| 1000th execution | 100ms | 5ms | **95%** |

*Note: Actual performance gains depend on query complexity and system configuration.*

### Best Practices

1. **Compile Once, Use Many Times**
   ```csharp
   // ❌ Bad - recompiles every time
   for (int i = 0; i < 1000; i++)
   {
       var query = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 25));
       var results = await query.ExecuteAsync(client);
   }
   
   // ✅ Good - compile once, reuse
   var query = CompiledQuery.Create<User>("users", q => q.Where(u => u.Age > 25));
   for (int i = 0; i < 1000; i++)
   {
       var results = await query.ExecuteAsync(client);
   }
   ```

2. **Store Compiled Queries as Fields**
   ```csharp
   public class UserRepository
   {
       private static readonly CompiledQuery<User, List<User>> _activeUsersQuery = 
           CompiledQuery.Create<User>("users", q => q.Where(u => u.IsActive));
       
       public async Task<List<User>> GetActiveUsersAsync()
       {
           return await _activeUsersQuery.ExecuteAsync(_client);
       }
   }
   ```

3. **Use for API Endpoints**
   ```csharp
   public class ProductsController
   {
       private static readonly CompiledQuery<Product, List<Product>> _recentProductsQuery =
           CompiledQuery.Create<Product>("products", 
               q => q.OrderByDescending(p => p.CreatedAt).Take(20));
       
       [HttpGet("recent")]
       public async Task<IActionResult> GetRecentProducts()
       {
           var products = await _recentProductsQuery.ExecuteAsync(_client);
           return Ok(products);
       }
   }
   ```

## Cancellation Support

CompiledQuery respects cancellation tokens:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var results = await compiledQuery.ExecuteAsync(client, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Query was cancelled");
}
```

## Limitations

1. **Parameter Capture**: Parameters are captured from closures at compilation time, not at execution time
2. **Static Cache**: The cache is static and shared across all instances
3. **No TTL**: Cached queries don't expire automatically (use `ClearCache()` if needed)
4. **Memory Usage**: Very large caches may impact memory; monitor with `GetStatistics()`

## Thread Safety

CompiledQuery is fully thread-safe:
- Cache operations use `ConcurrentDictionary`
- Statistics use atomic operations (`Interlocked`)
- Multiple threads can compile and execute queries concurrently

```csharp
// Safe to use from multiple threads
await Task.WhenAll(
    Task.Run(() => compiledQuery.ExecuteAsync(client)),
    Task.Run(() => compiledQuery.ExecuteAsync(client)),
    Task.Run(() => compiledQuery.ExecuteAsync(client))
);
```

## Integration with Other Features

### With IAsyncEnumerable (Streaming)

CompiledQuery returns materialized lists. For streaming, use regular queries:

```csharp
// Use ToAsyncEnumerable() for streaming (not compiled)
await foreach (var user in client.Query<User>("users")
    .Where(u => u.Age > 25)
    .ToAsyncEnumerable())
{
    // Process user
}
```

### With GroupBy/Aggregations

```csharp
var compiledQuery = CompiledQuery.Create<User, AgeGroup>(
    "users",
    q => q.GroupBy(u => u.Age)
          .Select(g => new AgeGroup
          {
              Age = g.Key,
              Count = g.Count()
          })
);
```

## See Also

- [LINQ Query Builder](query-builder.md)
- [IQueryable<T>](iqueryable.md)
- [Async Streaming](async-streaming.md)
- [Expression Trees](expression-trees.md)
