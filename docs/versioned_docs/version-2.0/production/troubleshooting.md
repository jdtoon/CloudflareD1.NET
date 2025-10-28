---
sidebar_position: 2
---

# Troubleshooting Guide

This guide covers common errors, their causes, and solutions when working with CloudflareD1.NET.

## Connection Issues

### 401 Unauthorized

**Error Message:**
```
D1ApiException: D1 API request failed with status 401
```

**Causes:**
1. Invalid API Token
2. Expired API Token
3. Incorrect token format
4. Insufficient permissions

**Solutions:**

```csharp
// 1. Verify token is correctly set
var options = new D1Options
{
    ApiToken = "your-token-here", // Check for typos, extra spaces
    AccountId = "your-account-id",
    DatabaseId = "your-database-id"
};

// 2. Check token permissions in Cloudflare dashboard
// Required: Account → D1 → Edit (or Read for read-only)

// 3. Generate new token if expired
// https://dash.cloudflare.com/profile/api-tokens

// 4. Test token separately
var client = new D1Client(Options.Create(options), logger);
var health = await client.CheckHealthAsync();
if (!health.IsHealthy)
{
    Console.WriteLine($"Error: {health.ErrorMessage}");
}
```

### 404 Not Found

**Error Message:**
```
D1ApiException: D1 API request failed with status 404
```

**Causes:**
1. Invalid Account ID
2. Invalid Database ID
3. Database doesn't exist
4. Wrong API endpoint

**Solutions:**

```csharp
// 1. Verify Account ID
// Found at: https://dash.cloudflare.com/ → Select account → Copy ID from URL
var accountId = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"; // 32 characters

// 2. Verify Database ID
// Found at: D1 Dashboard → Select database → Copy UUID
var databaseId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"; // UUID format

// 3. List available databases to verify
var managementClient = client as ID1ManagementClient;
var databases = await managementClient.ListDatabasesAsync();
foreach (var db in databases.Result)
{
    Console.WriteLine($"Database: {db.Name}, ID: {db.Uuid}");
}

// 4. Check API base URL
options.ApiBaseUrl = "https://api.cloudflare.com/client/v4/"; // Default
```

### Network/Timeout Errors

**Error Message:**
```
D1ApiException: D1 API request timed out
HttpRequestException: Failed to communicate with D1 API
```

**Causes:**
1. Network connectivity issues
2. Firewall blocking outbound HTTPS
3. Timeout too short for slow queries
4. DNS resolution failure

**Solutions:**

```csharp
// 1. Increase timeout for slow queries
services.AddCloudflareD1(options =>
{
    options.TimeoutSeconds = 60; // Default: 30
});

// 2. Test network connectivity
var health = await client.CheckHealthAsync();
Console.WriteLine($"Latency: {health.LatencyMs}ms");

// 3. Check firewall allows HTTPS to api.cloudflare.com
// Port: 443

// 4. Enable detailed logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 5. Test with retry disabled to see raw errors
options.EnableRetry = false;
```

### Rate Limiting (429)

**Error Message:**
```
D1ApiException: D1 API request failed with status 429
```

**Cause:** Too many requests to Cloudflare API

**Solutions:**

```csharp
// 1. Retry is automatic (enabled by default)
options.EnableRetry = true;
options.MaxRetries = 5; // Increase for rate limits

// 2. Implement client-side throttling
private readonly SemaphoreSlim _throttle = new(10); // 10 concurrent requests
await _throttle.WaitAsync();
try
{
    var result = await client.QueryAsync(sql);
}
finally
{
    _throttle.Release();
}

// 3. Use batch operations to reduce request count
var statements = queries.Select(q => new D1Statement { Sql = q }).ToList();
await client.BatchAsync(statements); // Single request for multiple queries

// 4. Implement caching for frequently accessed data
_cache.GetOrCreateAsync($"key:{id}", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await client.QueryAsync<Entity>("SELECT * FROM entities WHERE id = ?", id);
});
```

## Query Failures

### SQL Syntax Errors

**Error Message:**
```
D1QueryException: SQL syntax error near "..."
```

**Causes:**
1. Invalid SQL syntax
2. SQLite vs standard SQL differences
3. Missing/mismatched quotes

**Solutions:**

```csharp
// 1. Test query locally first
var localOptions = new D1Options { UseLocalMode = true };
var localClient = new D1Client(Options.Create(localOptions), logger);

// 2. Use parameterized queries (prevents syntax issues)
// ❌ Bad - string concatenation
var sql = $"SELECT * FROM users WHERE name = '{name}'";

// ✅ Good - parameterized
var sql = "SELECT * FROM users WHERE name = ?";
var result = await client.QueryAsync(sql, new[] { name });

// 3. Check SQLite-specific syntax
// SQLite uses AUTOINCREMENT, not AUTO_INCREMENT
// SQLite uses INTEGER PRIMARY KEY, not SERIAL
// SQLite date functions: date('now'), datetime('now')

// 4. Enable debug logging to see actual SQL
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Parameter Mismatch

**Error Message:**
```
Wrong number of parameters
Parameter count mismatch
```

**Solutions:**

```csharp
// 1. Match parameter count
var sql = "SELECT * FROM users WHERE name = ? AND age = ?";
var params = new object[] { "John", 25 }; // 2 params for 2 placeholders

// 2. Use named parameters
var sql = "SELECT * FROM users WHERE name = @name AND age = @age";
var params = new { name = "John", age = 25 };

// 3. For LINQ queries, parameters are automatic
var users = await client.Query<User>("users")
    .Where(u => u.Name == "John" && u.Age == 25)
    .ToListAsync();

// 4. Check parameter array length
if (parameters is object[] arr)
{
    Console.WriteLine($"Parameter count: {arr.Length}");
}
```

### Large Result Sets

**Error Message:**
```
OutOfMemoryException
Response too large
```

**Causes:**
1. Loading too many rows at once
2. Large BLOB/TEXT columns
3. Inefficient queries

**Solutions:**

```csharp
// 1. Use pagination
var pageSize = 100;
var page = 0;
var users = await client.Query<User>("users")
    .OrderBy(u => u.Id)
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToListAsync();

// 2. Use async streaming for large datasets
await foreach (var user in client.Query<User>("users").ToAsyncEnumerable())
{
    // Process one at a time (memory efficient)
    await ProcessUserAsync(user);
}

// 3. Project only needed columns
var names = await client.Query<User>("users")
    .Select(u => new { u.Id, u.Name }) // Only 2 columns
    .ToListAsync();

// 4. Add WHERE clause to filter
var activeUsers = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .ToListAsync();

// 5. Check query performance
var result = await client.QueryAsync("SELECT * FROM large_table");
Console.WriteLine($"Duration: {result.Meta?.Duration}ms");
Console.WriteLine($"Rows: {result.Results?.Count}");
```

## Migration Problems

### Pending Migrations

**Error Message:**
```
Database schema out of sync
Pending migrations exist
```

**Solutions:**

```csharp
// 1. Check migration status
var runner = new MigrationRunner(client, "Migrations");
var applied = await runner.GetAppliedMigrationsAsync();
var pending = runner.GetPendingMigrations();

Console.WriteLine($"Applied: {applied.Count}");
Console.WriteLine($"Pending: {pending.Count}");

// 2. Apply pending migrations
var appliedMigrations = await runner.MigrateAsync();
Console.WriteLine($"Applied {appliedMigrations.Count} migrations");

// 3. Rollback if needed
await runner.RollbackAsync("MigrationId");

// 4. Check migration history
var history = await client.QueryAsync("SELECT * FROM __migrations ORDER BY id DESC");
```

### Migration Conflicts

**Error Message:**
```
Column already exists
Table already exists
Cannot drop column
```

**Solutions:**

```csharp
// 1. Check current schema
var columns = await client.QueryAsync("PRAGMA table_info(tablename)");

// 2. Use IF NOT EXISTS
builder.CreateTable("users", t => t
    .Integer("id").PrimaryKey().AutoIncrement()
    .Text("name").NotNull()
).IfNotExists();

// 3. Handle dropped columns (SQLite requires table recreation)
// See: docs/migrations/advanced.md

// 4. Generate fresh migration
dotnet d1 add MyMigration

// 5. For Code-First, check model diff
var generator = new CodeFirstMigrationGenerator("Migrations");
var summary = await generator.GetChangesSummaryAsync(context);
Console.WriteLine(summary);
```

### Rollback Failures

**Error Message:**
```
Cannot rollback: migration not found
Rollback failed: table in use
```

**Solutions:**

```csharp
// 1. Check migration exists
var applied = await runner.GetAppliedMigrationsAsync();
if (applied.Any(m => m.Id == targetMigration))
{
    await runner.RollbackAsync(targetMigration);
}

// 2. Close all connections before rollback (local mode)
await client.DisposeAsync();
var newClient = CreateNewClient();
await runner.RollbackAsync(targetMigration);

// 3. Check Down() method is implemented
public override void Down(MigrationBuilder builder)
{
    builder.DropTable("users"); // Must be implemented
}

// 4. Manual rollback if needed
await client.ExecuteAsync("DROP TABLE IF EXISTS problem_table");
await client.ExecuteAsync("DELETE FROM __migrations WHERE id = 'migration-id'");
```

## Performance Issues

### Slow Queries

**Symptoms:** Queries taking >1 second

**Solutions:**

```csharp
// 1. Add indexes
await client.ExecuteAsync(@"
    CREATE INDEX IF NOT EXISTS idx_users_email 
    ON users(email)
");

// 2. Use EXPLAIN to analyze query plan
var explain = await client.QueryAsync("EXPLAIN QUERY PLAN SELECT * FROM users WHERE email = ?", email);

// 3. Avoid SELECT *
// ❌ Slow
var users = await client.QueryAsync("SELECT * FROM users");

// ✅ Fast
var users = await client.QueryAsync("SELECT id, name FROM users");

// 4. Use compiled queries for repeated queries
var compiledQuery = CompiledQuery.Create<User>(
    "users",
    q => q.Where(u => u.Age > 25)
);
// 95% faster on subsequent executions

// 5. Check query duration in logs
// Logged automatically at Information level
```

### N+1 Query Problem

**Symptom:** Many queries in a loop

```csharp
// ❌ N+1 Problem (1 + N queries)
var users = await client.Query<User>("users").ToListAsync();
foreach (var user in users)
{
    var posts = await client.Query<Post>("posts")
        .Where(p => p.UserId == user.Id)
        .ToListAsync(); // Separate query for each user!
}
```

**Solutions:**

```csharp
// ✅ Solution 1: Join
var userPosts = await client.Query<User>("users")
    .Join(
        client.Query<Post>("posts"),
        user => user.Id,
        post => post.UserId
    )
    .Select((user, post) => new { User = user, Post = post })
    .ToListAsync();

// ✅ Solution 2: Load all, group in memory
var users = await client.Query<User>("users").ToListAsync();
var posts = await client.Query<Post>("posts").ToListAsync();
var userPosts = users.GroupJoin(posts, u => u.Id, p => p.UserId, (u, posts) => new { User = u, Posts = posts });

// ✅ Solution 3: Batch query with IN clause
var userIds = users.Select(u => u.Id).ToArray();
var posts = await client.Query<Post>("posts")
    .Where(p => userIds.Contains(p.UserId))
    .ToListAsync();
```

### Memory Issues

**Error Message:**
```
OutOfMemoryException
Process consuming excessive memory
```

**Solutions:**

```csharp
// 1. Use async streaming for large result sets
await foreach (var item in query.ToAsyncEnumerable())
{
    await ProcessItemAsync(item);
    // Item is garbage collected after processing
}

// 2. Use pagination
for (int page = 0; page < totalPages; page++)
{
    var items = await query
        .Skip(page * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    await ProcessBatchAsync(items);
    items.Clear(); // Allow GC
}

// 3. Project only needed columns
var ids = await client.Query<Entity>("entities")
    .Select(e => e.Id) // Just one column
    .ToListAsync();

// 4. Batch process with smaller chunks
var batch = new List<Entity>(100);
await foreach (var item in query.ToAsyncEnumerable())
{
    batch.Add(item);
    if (batch.Count >= 100)
    {
        await ProcessBatchAsync(batch);
        batch.Clear();
    }
}
```

## Code-First Issues

### Foreign Key Violations

**Error Message:**
```
FOREIGN KEY constraint failed
```

**Causes:**
1. Inserting child before parent
2. Deleting parent before children
3. Invalid foreign key value

**Solutions:**

```csharp
// 1. FK-aware ordering is automatic (v1.0.2+)
// Parents inserted before children automatically
context.Users.Add(user);
context.Posts.Add(new Post { UserId = user.Id, AuthorId = user.Id }); // Even if added first
await context.SaveChangesAsync(); // Correct order applied automatically

// 2. Manual ordering if needed
await context.Users.AddAsync(parent);
await context.SaveChangesAsync(); // Save parent first

await context.Posts.AddAsync(new Post { UserId = parent.Id });
await context.SaveChangesAsync(); // Then child

// 3. Check FK relationships in model
[ForeignKey("UserId")]
public User? User { get; set; } // Navigation property

[ForeignKey(nameof(UserId))] // Or use nameof
public int UserId { get; set; } // FK property

// 4. Enable cascade delete
builder.Entity<Post>()
    .HasOne(p => p.User)
    .WithMany(u => u.Posts)
    .OnDelete(DeleteBehavior.Cascade);
```

### Property Changes Not Detected

**Symptom:** Updates don't save

**Causes:**
1. Entity not tracked
2. Property not modified
3. Snapshot not captured

**Solutions:**

```csharp
// 1. Ensure entity is tracked
var user = await context.Users.FindAsync(1); // Tracked
user.Name = "Updated";
await context.SaveChangesAsync(); // Will update

// 2. Use Update() to track untracked entities
var user = new User { Id = 1, Name = "Updated" };
context.Users.Update(user); // Now tracked
await context.SaveChangesAsync();

// 3. Check entity state
var entry = context.ChangeTracker.GetEntry(user);
Console.WriteLine($"State: {entry.State}");
Console.WriteLine($"Modified properties: {string.Join(", ", entry.GetModifiedProperties())}");

// 4. Manual tracking if needed
context.ChangeTracker.TrackUpdate(user);
```

### Snapshot Issues

**Error Message:**
```
Cannot generate migration: snapshot missing
Model changes not detected
```

**Solutions:**

```csharp
// 1. Generate initial migration to create snapshot
dotnet d1 add-codefirst InitialCreate --context MyDbContext

// 2. Check snapshot file exists
// Location: Migrations/.migrations-snapshot.json

// 3. Regenerate snapshot if corrupted
// Delete Migrations/.migrations-snapshot.json
// Run: dotnet d1 add-codefirst RecreateSnapshot --context MyDbContext

// 4. Check model changes
var generator = new CodeFirstMigrationGenerator("Migrations");
var hasChanges = await generator.HasPendingChangesAsync(context);
var summary = await generator.GetChangesSummaryAsync(context);
Console.WriteLine(summary);
```

## Debug Logging

### Enable Detailed Logging

```csharp
// Program.cs / Startup.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Filter to D1 only
builder.Logging.AddFilter("CloudflareD1", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
```

**Example Debug Output:**
```
dbug: CloudflareD1.NET.D1Client[0]
      Executing query in remote mode: SELECT * FROM users
dbug: CloudflareD1.NET.Providers.CloudflareD1Provider[0]
      Request payload: {"sql":"SELECT * FROM users","params":null}
dbug: CloudflareD1.NET.Providers.CloudflareD1Provider[0]
      Response status: 200, Content: {"success":true,"result":[...],"meta":{...}}
info: CloudflareD1.NET.Providers.CloudflareD1Provider[0]
      D1 query executed successfully, returned 5 result(s) (Duration: 123ms)
```

### Request/Response Inspection

```csharp
// Add HTTP logging middleware
builder.Services.AddHttpClient<D1Client>()
    .AddHttpMessageHandler(() => new LoggingHandler(logger));

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log request
        Console.WriteLine($"Request: {request.Method} {request.RequestUri}");
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            Console.WriteLine($"Body: {content}");
        }
        
        var response = await base.SendAsync(request, cancellationToken);
        
        // Log response
        Console.WriteLine($"Response: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Body: {responseContent}");
        
        return response;
    }
}
```

## Getting Help

If you're still stuck:

1. **Check documentation**: https://jdtoon.github.io/CloudflareD1.NET/
2. **Enable debug logging** and examine output
3. **Test with health check**: `await client.CheckHealthAsync()`
4. **Try local mode** to isolate network/API issues
5. **Review examples**: https://github.com/jdtoon/CloudflareD1.NET/tree/main/samples
6. **Create GitHub issue**: https://github.com/jdtoon/CloudflareD1.NET/issues

## Next Steps

- [Production Deployment Guide](./deployment.md) - Best practices for production
- [Performance Tuning Guide](./performance.md) - Optimization techniques
