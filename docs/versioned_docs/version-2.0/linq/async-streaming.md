---
sidebar_position: 10
---

# Async Streaming

Stream query results efficiently using `IAsyncEnumerable<T>` for memory-efficient processing of large datasets. Process results one at a time without loading everything into memory.

## Overview

**ToAsyncEnumerable()** returns an async stream that yields entities one at a time, making it ideal for:
- Processing large result sets that don't fit in memory
- Early termination (stop processing when you find what you need)
- Real-time processing as results arrive
- Memory-constrained environments

:::info Version
Async streaming with `IAsyncEnumerable<T>` was introduced in **v1.9.0-beta**.
:::

## Basic Usage

### Simple Streaming

```csharp
// Stream all users - processes one at a time
await foreach (var user in client.Query<User>("users").ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
    // Each user is processed individually
    // Memory is freed after processing each one
}
```

### With Cancellation

```csharp
var cts = new CancellationTokenSource();

try
{
    await foreach (var user in client.Query<User>("users")
        .ToAsyncEnumerable(cts.Token))
    {
        await ProcessUserAsync(user);
        
        // Can cancel at any point
        if (shouldStop)
        {
            cts.Cancel();
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Streaming cancelled");
}
```

## Memory Efficiency

### Problem: Loading Everything

```csharp
// ❌ Memory Issue - Loads ALL users into memory at once
var users = await client.Query<User>("users").ToListAsync();
foreach (var user in users) // 100,000 users in memory!
{
    await ProcessUserAsync(user);
}
```

### Solution: Stream Results

```csharp
// ✅ Memory Efficient - Only one user in memory at a time
await foreach (var user in client.Query<User>("users").ToAsyncEnumerable())
{
    await ProcessUserAsync(user); // Process and release
}
```

**Memory Usage:**
- `ToListAsync()`: O(n) - all records in memory
- `ToAsyncEnumerable()`: O(1) - only current record in memory

## Filtering and Ordering

### With WHERE Clause

```csharp
// Stream only active users
await foreach (var user in client.Query<User>("users")
    .Where(u => u.IsActive)
    .ToAsyncEnumerable())
{
    await SendNotificationAsync(user);
}
```

### With ORDER BY

```csharp
// Stream users ordered by registration date
await foreach (var user in client.Query<User>("users")
    .OrderBy(u => u.RegisteredAt)
    .ToAsyncEnumerable())
{
    Console.WriteLine($"Processing: {user.Name}");
}
```

### Complex Queries

```csharp
// Stream with multiple conditions
await foreach (var user in client.Query<User>("users")
    .Where(u => u.IsActive)
    .Where(u => u.Age >= 18)
    .OrderByDescending(u => u.LastLoginAt)
    .ToAsyncEnumerable())
{
    await UpdateUserStatusAsync(user);
}
```

## Pagination with Streaming

### Processing in Batches

```csharp
// Process first 1000 users
await foreach (var user in client.Query<User>("users")
    .Take(1000)
    .ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
}
```

### Skip and Take

```csharp
// Process page 3 (users 201-300)
await foreach (var user in client.Query<User>("users")
    .OrderBy(u => u.Id)
    .Skip(200)
    .Take(100)
    .ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
}
```

## Early Termination

One of the key benefits - stop processing when you find what you need:

### Break on Condition

```csharp
// Find first 5 premium users
var premiumCount = 0;
await foreach (var user in client.Query<User>("users")
    .Where(u => u.IsPremium)
    .ToAsyncEnumerable())
{
    await ProcessPremiumUserAsync(user);
    
    premiumCount++;
    if (premiumCount >= 5)
    {
        break; // Stop streaming - don't process remaining users
    }
}
```

### Search and Stop

```csharp
// Find specific user and stop
User? foundUser = null;
await foreach (var user in client.Query<User>("users").ToAsyncEnumerable())
{
    if (user.Email == targetEmail)
    {
        foundUser = user;
        break; // Found it - stop streaming
    }
}
```

## Cancellation Patterns

### Timeout Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await foreach (var user in client.Query<User>("users")
        .ToAsyncEnumerable(cts.Token))
    {
        await ProcessUserAsync(user);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Processing timed out after 30 seconds");
}
```

### User-Triggered Cancellation

```csharp
var cts = new CancellationTokenSource();

// Wire up cancel button
cancelButton.Click += (s, e) => cts.Cancel();

try
{
    await foreach (var user in client.Query<User>("users")
        .ToAsyncEnumerable(cts.Token))
    {
        await ProcessUserAsync(user);
        await UpdateProgressBarAsync();
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Processing cancelled by user");
}
```

### Combined Cancellation

```csharp
// Cancel on timeout OR user action
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    timeoutCts.Token, 
    userCancellationToken);

await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable(linkedCts.Token))
{
    await ProcessUserAsync(user);
}
```

## Real-World Use Cases

### Batch Processing

```csharp
// Process 100,000 users in batches
var batch = new List<User>();
const int batchSize = 100;

await foreach (var user in client.Query<User>("users")
    .Where(u => u.IsActive)
    .ToAsyncEnumerable())
{
    batch.Add(user);
    
    if (batch.Count >= batchSize)
    {
        await ProcessBatchAsync(batch);
        batch.Clear(); // Free memory
    }
}

// Process remaining items
if (batch.Count > 0)
{
    await ProcessBatchAsync(batch);
}
```

### Export to File

```csharp
// Export users to CSV without loading all into memory
await using var writer = new StreamWriter("users.csv");
await writer.WriteLineAsync("Id,Name,Email,Age");

await foreach (var user in client.Query<User>("users")
    .OrderBy(u => u.Id)
    .ToAsyncEnumerable())
{
    await writer.WriteLineAsync($"{user.Id},{user.Name},{user.Email},{user.Age}");
}
```

### Real-time Processing

```csharp
// Process and display results as they arrive
await foreach (var user in client.Query<User>("users")
    .Where(u => u.CreatedAt > lastProcessedDate)
    .OrderBy(u => u.CreatedAt)
    .ToAsyncEnumerable())
{
    Console.WriteLine($"Processing: {user.Name}");
    await SendWelcomeEmailAsync(user);
    await UpdateProgressAsync();
}
```

### Data Migration

```csharp
// Migrate users from old to new system
var migratedCount = 0;
var errors = new List<string>();

await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    try
    {
        await MigrateUserToNewSystemAsync(user);
        migratedCount++;
    }
    catch (Exception ex)
    {
        errors.Add($"Failed to migrate {user.Id}: {ex.Message}");
    }
    
    if (migratedCount % 100 == 0)
    {
        Console.WriteLine($"Migrated {migratedCount} users...");
    }
}

Console.WriteLine($"Migration complete: {migratedCount} users");
if (errors.Any())
{
    Console.WriteLine($"Errors: {errors.Count}");
}
```

## Performance Comparison

### Scenario: Processing 1 Million Records

| Method | Memory Usage | Time to First Result | Total Time |
|--------|--------------|---------------------|------------|
| `ToListAsync()` | ~500 MB | 5 seconds | 10 seconds |
| `ToAsyncEnumerable()` | ~5 KB | &lt;1 ms | 10 seconds |

**Key Benefits:**
- **99% less memory** - only current record in memory
- **Instant start** - begin processing immediately
- **Interruptible** - can stop at any time

## CancellationToken Support

All async methods now support `CancellationToken`:

```csharp
var cts = new CancellationTokenSource();

// ToListAsync with cancellation
var users = await client.Query<User>("users")
    .ToListAsync(cts.Token);

// FirstOrDefaultAsync with cancellation
var firstUser = await client.Query<User>("users")
    .FirstOrDefaultAsync(cts.Token);

// CountAsync with cancellation
var count = await client.Query<User>("users")
    .CountAsync(cts.Token);

// AnyAsync with cancellation
var hasUsers = await client.Query<User>("users")
    .AnyAsync(cts.Token);

// ToAsyncEnumerable with cancellation
await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable(cts.Token))
{
    // Process user
}
```

## Best Practices

### 1. Use for Large Result Sets

```csharp
// ✅ Good - Large datasets
await foreach (var user in client.Query<User>("users").ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
}

// ❌ Not necessary - Small datasets
await foreach (var user in client.Query<User>("users")
    .Take(10)
    .ToAsyncEnumerable())
{
    // Just use ToListAsync() for small sets
}
```

### 2. Always Use Cancellation Tokens

```csharp
// ✅ Good - Allows cancellation
public async Task ProcessUsersAsync(CancellationToken cancellationToken)
{
    await foreach (var user in client.Query<User>("users")
        .ToAsyncEnumerable(cancellationToken))
    {
        await ProcessUserAsync(user);
    }
}

// ❌ Bad - Cannot be cancelled
public async Task ProcessUsersAsync()
{
    await foreach (var user in client.Query<User>("users")
        .ToAsyncEnumerable())
    {
        await ProcessUserAsync(user); // Runs to completion
    }
}
```

### 3. Order Results for Consistency

```csharp
// ✅ Good - Predictable order
await foreach (var user in client.Query<User>("users")
    .OrderBy(u => u.Id)
    .ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
}

// ❌ Unpredictable - No ordering specified
await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    // Order may vary between runs
}
```

### 4. Handle Errors Appropriately

```csharp
// ✅ Good - Error handling
await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    try
    {
        await ProcessUserAsync(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process user {UserId}", user.Id);
        // Continue with next user
    }
}
```

### 5. Batch for Efficiency When Needed

```csharp
// ✅ Good - Batch database writes
var batch = new List<User>();
await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    user.ProcessedAt = DateTime.UtcNow;
    batch.Add(user);
    
    if (batch.Count >= 100)
    {
        await SaveBatchAsync(batch); // One DB call for 100 users
        batch.Clear();
    }
}
```

## Common Patterns

### Progress Reporting

```csharp
var processed = 0;
var total = await client.Query<User>("users").CountAsync();

await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    await ProcessUserAsync(user);
    processed++;
    
    if (processed % 100 == 0)
    {
        var progress = (double)processed / total * 100;
        Console.WriteLine($"Progress: {progress:F1}%");
    }
}
```

### Conditional Processing

```csharp
await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    if (user.IsPremium)
    {
        await ProcessPremiumUserAsync(user);
    }
    else
    {
        await ProcessStandardUserAsync(user);
    }
}
```

### Aggregating While Streaming

```csharp
var totalAge = 0;
var count = 0;

await foreach (var user in client.Query<User>("users")
    .ToAsyncEnumerable())
{
    totalAge += user.Age;
    count++;
}

var averageAge = count > 0 ? (double)totalAge / count : 0;
Console.WriteLine($"Average age: {averageAge:F1}");
```

## Comparison with Other Methods

### When to Use Each

| Method | Use When | Memory | Speed |
|--------|----------|--------|-------|
| `ToAsyncEnumerable()` | Processing large datasets, need early termination | O(1) | Fast start |
| `ToListAsync()` | Need all results in memory, small datasets | O(n) | Fast for small |
| `FirstOrDefaultAsync()` | Only need first result | O(1) | Fastest |
| `CountAsync()` | Only need count | O(1) | Optimized |

## Limitations

1. **No Random Access**: Can't index into results like `results[10]`
2. **Single Enumeration**: Best used once; re-enumerate executes query again
3. **No Materialized Operations**: Can't use LINQ methods that need all data (like `OrderBy` on the enumerable itself)

## See Also

- [Query Builder](./query-builder.md) - Basic query operations
- [Existence Checks](./existence-checks.md) - AnyAsync and AllAsync with predicates
- [Set Operations](./set-operations.md) - Union, Intersect, Except
