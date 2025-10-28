---
sidebar_position: 3
---

# Performance Tuning Guide

This guide provides techniques and best practices for optimizing CloudflareD1.NET performance in production environments.

## Query Optimization

### Use Indexes

Indexes dramatically improve query performance for WHERE, JOIN, and ORDER BY clauses.

```csharp
// Create indexes during migration
public override void Up(MigrationBuilder builder)
{
    builder.CreateTable("users", t => t
        .Integer("id").PrimaryKey().AutoIncrement()
        .Text("email").NotNull().Unique()
        .Text("name").NotNull()
        .Integer("age")
    );
    
    // Add indexes for frequently queried columns
    builder.CreateIndex("idx_users_email", "users", "email");
    builder.CreateIndex("idx_users_age", "users", "age");
    builder.CreateIndex("idx_users_name_age", "users", new[] { "name", "age" }); // Composite
}

// Check if index is being used
var plan = await client.QueryAsync(@"
    EXPLAIN QUERY PLAN 
    SELECT * FROM users WHERE email = ?
", "test@example.com");

// Expected output: "SEARCH users USING INDEX idx_users_email (email=?)"
// Bad output: "SCAN users" (no index used)
```

**Index Guidelines:**
- Index columns used in WHERE clauses
- Index foreign keys used in JOINs
- Index columns used in ORDER BY (especially with LIMIT)
- Don't over-index (slows INSERT/UPDATE)
- Composite indexes for multi-column queries

### Analyze Query Plans

Use EXPLAIN QUERY PLAN to understand how SQLite executes queries:

```csharp
// Check query execution plan
var query = "SELECT * FROM users u JOIN posts p ON u.id = p.user_id WHERE u.age > 25";
var plan = await client.QueryAsync($"EXPLAIN QUERY PLAN {query}");

foreach (var row in plan.Results)
{
    Console.WriteLine($"Step: {row.Detail}");
}
```

**Interpreting Results:**
- `SCAN table`: ❌ Full table scan (slow for large tables)
- `SEARCH table USING INDEX`: ✅ Index used (fast)
- `USE TEMP B-TREE FOR ORDER BY`: ⚠️ Sorting needed (consider index)
- `USE TEMP B-TREE FOR GROUP BY`: ⚠️ Grouping needed (consider index)

### Select Only Needed Columns

Avoid `SELECT *` in production code:

```csharp
// ❌ Inefficient - loads all columns
var users = await client.QueryAsync("SELECT * FROM users");

// ✅ Efficient - only needed columns
var users = await client.QueryAsync("SELECT id, name FROM users");

// LINQ projection
var userNames = await client.Query<User>("users")
    .Select(u => new { u.Id, u.Name }) // Only 2 columns
    .ToListAsync();
```

**Benefits:**
- Reduces data transfer from D1 API
- Decreases JSON deserialization time
- Lowers memory consumption
- Faster response times

### Use Parameterized Queries

Parameterized queries are safer and can be more efficient:

```csharp
// ❌ String concatenation (unsafe + no query caching)
var sql = $"SELECT * FROM users WHERE name = '{name}' AND age = {age}";

// ✅ Parameterized (safe + cacheable)
var sql = "SELECT * FROM users WHERE name = ? AND age = ?";
var result = await client.QueryAsync(sql, new object[] { name, age });

// ✅ Named parameters
var result = await client.QueryAsync(
    "SELECT * FROM users WHERE name = @name AND age = @age",
    new { name, age }
);
```

### Limit Result Sets

Always use LIMIT for large tables:

```csharp
// ❌ Can return millions of rows
var allUsers = await client.QueryAsync("SELECT * FROM users");

// ✅ Paginated results
var page = 1;
var pageSize = 100;
var users = await client.Query<User>("users")
    .OrderBy(u => u.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ✅ Get count separately (more efficient)
var count = await client.Query<User>("users").CountAsync();
var totalPages = (int)Math.Ceiling(count / (double)pageSize);
```

## Compiled Queries

Compiled queries cache the query plan and expression tree, providing ~95% performance improvement for repeated queries.

### When to Use Compiled Queries

```csharp
// ✅ Good candidates for compilation
// 1. Repeated queries with different parameters
var getUserById = CompiledQuery.Create<User>(
    "users",
    (int id) => q => q.Where(u => u.Id == id)
);

// 2. Complex queries executed frequently
var getActiveUsersByAge = CompiledQuery.Create<User>(
    "users",
    (int minAge) => q => q
        .Where(u => u.IsActive && u.Age >= minAge)
        .OrderByDescending(u => u.CreatedAt)
);

// 3. Queries in hot paths (API endpoints, background jobs)
// 4. Queries with complex joins/filters

// ❌ Don't compile
// 1. One-off queries (migration scripts, admin tools)
// 2. Dynamic queries (WHERE clause varies significantly)
// 3. Queries executed infrequently
```

### Compiled Query Examples

```csharp
// Simple filter
var getUserByEmail = CompiledQuery.Create<User>(
    "users",
    (string email) => q => q.Where(u => u.Email == email)
);

// Usage
var user = await getUserByEmail(client, "test@example.com");

// Multiple parameters
var searchUsers = CompiledQuery.Create<User>(
    "users",
    (string namePrefix, int minAge, int maxAge) => q => q
        .Where(u => u.Name.StartsWith(namePrefix) && u.Age >= minAge && u.Age <= maxAge)
        .OrderBy(u => u.Name)
);

var results = await searchUsers(client, "John", 25, 50);

// With projections
var getUserNames = CompiledQuery.Create<User, string>(
    "users",
    q => q.Select(u => u.Name)
);

var names = await getUserNames(client); // Returns List<string>

// Complex query
var getPostsWithAuthors = CompiledQuery.Create<Post, object>(
    "posts",
    q => q.Join(
        client.Query<User>("users"),
        p => p.UserId,
        u => u.Id,
        (post, user) => new { post.Title, AuthorName = user.Name }
    )
);
```

### Compilation Performance

| Query Type | First Execution | Subsequent (Compiled) | Improvement |
|------------|----------------|---------------------|-------------|
| Simple WHERE | 120ms | 6ms | **95% faster** |
| Complex JOIN | 250ms | 12ms | **95% faster** |
| Aggregation | 90ms | 5ms | **94% faster** |

## Batch Operations

Batch operations reduce API round trips significantly.

### Batch Inserts

```csharp
// ❌ Inefficient - N API requests
foreach (var user in users)
{
    await client.ExecuteAsync(
        "INSERT INTO users (name, email) VALUES (?, ?)",
        new object[] { user.Name, user.Email }
    );
}

// ✅ Efficient - 1 API request
var statements = users.Select(u => new D1Statement
{
    Sql = "INSERT INTO users (name, email) VALUES (?, ?)",
    Params = new object[] { u.Name, u.Email }
}).ToList();

var results = await client.BatchAsync(statements);

// ✅ Even better - Use INSERT with multiple values
var sql = "INSERT INTO users (name, email) VALUES " + 
          string.Join(", ", users.Select(_ => "(?, ?)"));
var params = users.SelectMany(u => new object[] { u.Name, u.Email }).ToArray();
await client.ExecuteAsync(sql, params);
```

### Batch Updates

```csharp
// ❌ N API requests
foreach (var user in usersToUpdate)
{
    await client.ExecuteAsync(
        "UPDATE users SET name = ? WHERE id = ?",
        new object[] { user.Name, user.Id }
    );
}

// ✅ 1 API request
var statements = usersToUpdate.Select(u => new D1Statement
{
    Sql = "UPDATE users SET name = ? WHERE id = ?",
    Params = new object[] { u.Name, u.Id }
}).ToList();

await client.BatchAsync(statements);
```

### Batch Limits

```csharp
// D1 API batch limit: typically 1000 statements
// Split large batches
const int batchSize = 500; // Safety margin
for (int i = 0; i < statements.Count; i += batchSize)
{
    var batch = statements.Skip(i).Take(batchSize).ToList();
    await client.BatchAsync(batch);
}
```

## Code-First Optimizations

### Per-Property Change Detection (v1.0.3+)

Only update modified properties instead of all properties:

```csharp
// Automatic per-property updates (v1.0.3+)
var user = await context.Users.FindAsync(1);
user.Name = "Updated"; // Only Name changed

await context.SaveChangesAsync();
// Generated SQL: UPDATE users SET name = ? WHERE id = ?
// (Not: UPDATE users SET name = ?, email = ?, age = ?, ... WHERE id = ?)

// Performance improvement
// - 10 properties, 1 changed: 90% less data sent
// - 50 properties, 2 changed: 96% less data sent
```

**Benefits:**
- Reduces UPDATE statement size
- Minimizes data transfer
- Improves concurrent update safety
- Automatically enabled in v1.0.3+

### Foreign Key Ordering (v1.0.2+)

Parents are automatically inserted before children:

```csharp
// Automatic FK ordering (v1.0.2+)
var user = new User { Name = "John" };
var post1 = new Post { Title = "Post 1", UserId = user.Id };
var post2 = new Post { Title = "Post 2", UserId = user.Id };

// Add in any order
context.Posts.Add(post1);
context.Posts.Add(post2);
context.Users.Add(user); // Added last

await context.SaveChangesAsync();
// Executes: INSERT INTO users (...); -- First
//           INSERT INTO posts (...); -- Then children
//           INSERT INTO posts (...);
```

**Benefits:**
- Prevents FK constraint violations
- No manual ordering needed
- Safer concurrent operations

### Efficient Bulk Operations

```csharp
// ✅ Bulk insert with AddRange
var users = Enumerable.Range(1, 1000)
    .Select(i => new User { Name = $"User {i}", Email = $"user{i}@test.com" })
    .ToList();

context.Users.AddRange(users); // More efficient than 1000 Add() calls
await context.SaveChangesAsync();

// ✅ Track updates in batch
var usersToUpdate = await context.Users
    .Where(u => u.Age < 18)
    .ToListAsync();

foreach (var user in usersToUpdate)
{
    user.Age = 18;
}

await context.SaveChangesAsync(); // Single batch update
```

### Relationship Loading

```csharp
// Configure relationships for optimal loading
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // ✅ Virtual for lazy loading (when needed)
    public virtual ICollection<Post> Posts { get; set; }
}

// Eager loading when needed
var users = await context.Users
    .Include(u => u.Posts) // Load related posts
    .ToListAsync();

// Projection for minimal data
var userPostCounts = await context.Users
    .Select(u => new { u.Name, PostCount = u.Posts.Count })
    .ToListAsync();
```

## Connection Management

### Retry Policy Configuration

```csharp
// Tune retry policy for your workload
services.AddCloudflareD1(options =>
{
    // High-traffic scenarios (more aggressive)
    options.EnableRetry = true;
    options.MaxRetries = 5;
    options.InitialRetryDelayMs = 100; // Exponential: 100, 200, 400, 800, 1600ms
    
    // Low-latency requirements (fewer retries)
    options.MaxRetries = 2;
    options.InitialRetryDelayMs = 50; // Faster retry
    
    // Batch operations (longer retry)
    options.MaxRetries = 3;
    options.InitialRetryDelayMs = 200;
});
```

### Timeout Configuration

```csharp
services.AddCloudflareD1(options =>
{
    // Adjust based on query complexity
    options.TimeoutSeconds = 30; // Default
    
    // For complex analytics queries
    options.TimeoutSeconds = 60;
    
    // For simple CRUD
    options.TimeoutSeconds = 10;
});

// Per-query timeout (if needed)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var result = await client.QueryAsync("SELECT ...", cancellationToken: cts.Token);
```

### Health Check Monitoring

```csharp
// Monitor connection health
var health = await client.CheckHealthAsync();

if (health.LatencyMs > 500)
{
    logger.LogWarning("High D1 latency: {Latency}ms", health.LatencyMs);
}

if (!health.IsHealthy)
{
    logger.LogError("D1 unhealthy: {Error}", health.ErrorMessage);
    // Trigger alerts, circuit breaker, etc.
}

// Periodic health checks
var timer = new Timer(async _ =>
{
    var health = await client.CheckHealthAsync();
    metrics.RecordLatency(health.LatencyMs);
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
```

## Caching Strategies

### In-Memory Caching

```csharp
// ASP.NET Core Memory Cache
services.AddMemoryCache();

public class UserService
{
    private readonly IMemoryCache _cache;
    private readonly ID1Client _client;
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _cache.GetOrCreateAsync($"user:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            
            return await _client.Query<User>("users")
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();
        });
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await _client.ExecuteAsync(
            "UPDATE users SET name = ?, email = ? WHERE id = ?",
            new object[] { user.Name, user.Email, user.Id }
        );
        
        // Invalidate cache
        _cache.Remove($"user:{user.Id}");
    }
}
```

### Distributed Caching

```csharp
// Redis for multi-instance scenarios
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "D1Cache";
});

public class CachedUserRepository
{
    private readonly IDistributedCache _cache;
    private readonly ID1Client _client;
    
    public async Task<User> GetUserAsync(int id)
    {
        var key = $"user:{id}";
        var cached = await _cache.GetStringAsync(key);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<User>(cached);
        }
        
        var user = await _client.Query<User>("users")
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();
        
        if (user != null)
        {
            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(user),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                }
            );
        }
        
        return user;
    }
}
```

### Query Result Caching

```csharp
// Cache expensive query results
private readonly ConcurrentDictionary<string, (DateTime Expires, object Result)> _queryCache = new();

public async Task<List<T>> GetCachedQueryAsync<T>(
    string cacheKey,
    Func<Task<List<T>>> query,
    TimeSpan cacheDuration)
{
    if (_queryCache.TryGetValue(cacheKey, out var cached) && cached.Expires > DateTime.UtcNow)
    {
        return (List<T>)cached.Result;
    }
    
    var result = await query();
    _queryCache[cacheKey] = (DateTime.UtcNow.Add(cacheDuration), result);
    
    return result;
}

// Usage
var activeUsers = await GetCachedQueryAsync(
    "active-users",
    () => client.Query<User>("users").Where(u => u.IsActive).ToListAsync(),
    TimeSpan.FromMinutes(5)
);
```

## Monitoring & Metrics

### Custom Performance Metrics

```csharp
public class D1MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<D1MetricsMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            
            // Log slow requests
            if (sw.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow request: {Method} {Path} took {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds
                );
            }
            
            // Record metrics
            Metrics.RequestDuration.Record(sw.ElapsedMilliseconds);
        }
    }
}
```

### Query Performance Tracking

```csharp
public class PerformanceTrackingClient : ID1Client
{
    private readonly ID1Client _inner;
    private readonly ILogger _logger;
    
    public async Task<D1QueryResult<T>> QueryAsync<T>(string sql, params object[] parameters)
    {
        var sw = Stopwatch.StartNew();
        var result = await _inner.QueryAsync<T>(sql, parameters);
        sw.Stop();
        
        // Track query performance
        _logger.LogInformation(
            "Query executed: {Duration}ms, {Rows} rows, SQL: {Sql}",
            sw.ElapsedMilliseconds,
            result.Results?.Count ?? 0,
            sql
        );
        
        // Alert on slow queries
        if (sw.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("Slow query detected: {Sql}", sql);
        }
        
        return result;
    }
}
```

## Benchmarking

### Load Testing

```csharp
// Example load test with BenchmarkDotNet
[MemoryDiagnoser]
public class D1Benchmarks
{
    private ID1Client _client;
    
    [GlobalSetup]
    public void Setup()
    {
        var options = new D1Options
        {
            ApiToken = Environment.GetEnvironmentVariable("D1_API_TOKEN"),
            AccountId = Environment.GetEnvironmentVariable("D1_ACCOUNT_ID"),
            DatabaseId = Environment.GetEnvironmentVariable("D1_DATABASE_ID")
        };
        _client = new D1Client(Options.Create(options), NullLogger<D1Client>.Instance);
    }
    
    [Benchmark]
    public async Task SimpleQuery()
    {
        await _client.QueryAsync("SELECT * FROM users LIMIT 10");
    }
    
    [Benchmark]
    public async Task ComplexQuery()
    {
        await _client.QueryAsync(@"
            SELECT u.name, COUNT(p.id) as post_count
            FROM users u
            LEFT JOIN posts p ON u.id = p.user_id
            WHERE u.age > 25
            GROUP BY u.id
            ORDER BY post_count DESC
            LIMIT 10
        ");
    }
    
    [Benchmark]
    public async Task BatchInsert()
    {
        var statements = Enumerable.Range(1, 100)
            .Select(i => new D1Statement
            {
                Sql = "INSERT INTO users (name, email) VALUES (?, ?)",
                Params = new object[] { $"User {i}", $"user{i}@test.com" }
            })
            .ToList();
        
        await _client.BatchAsync(statements);
    }
}

// Run: dotnet run -c Release --filter *D1Benchmarks*
```

### Profiling

```csharp
// Use Application Insights or similar for production profiling
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
});

// Track custom metrics
var telemetry = new TelemetryClient();
telemetry.TrackMetric("D1_QueryDuration", queryDuration);
telemetry.TrackMetric("D1_ResultCount", resultCount);
telemetry.TrackDependency("D1", "Query", sql, startTime, duration, success);
```

## Performance Checklist

- [ ] **Indexes created** for all frequently queried columns
- [ ] **Query plans analyzed** with EXPLAIN QUERY PLAN
- [ ] **SELECT * avoided** in production queries
- [ ] **Pagination implemented** for large result sets
- [ ] **Compiled queries used** for repeated queries (95% faster)
- [ ] **Batch operations** replace individual operations where possible
- [ ] **Per-property updates** enabled (automatic in v1.0.3+)
- [ ] **Caching strategy** implemented for frequently accessed data
- [ ] **Retry policy tuned** for workload (EnableRetry, MaxRetries)
- [ ] **Timeouts configured** appropriately (TimeoutSeconds)
- [ ] **Health checks** monitored regularly
- [ ] **Slow queries logged** and investigated
- [ ] **Load testing performed** under realistic conditions
- [ ] **Metrics collected** for query duration, result count, errors

## Performance Targets

| Operation | Target | Measurement |
|-----------|--------|-------------|
| Simple SELECT | &lt;100ms | 95th percentile |
| Complex JOIN | &lt;300ms | 95th percentile |
| INSERT/UPDATE | &lt;150ms | 95th percentile |
| Batch (100 ops) | &lt;500ms | 95th percentile |
| Health Check | &lt;200ms | Average |
| Compiled Query | &lt;10ms | Average (after compilation) |

## Next Steps

- [Production Deployment Guide](./deployment.md) - Deploy optimized setup
- [Troubleshooting Guide](./troubleshooting.md) - Debug performance issues
- [LINQ Documentation](../linq/intro.md) - Query optimization techniques
