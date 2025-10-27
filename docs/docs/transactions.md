# Transactions

CloudflareD1.NET v1.11.0+ provides comprehensive transaction support, allowing you to group multiple database operations into atomic units that succeed or fail together.

## Overview

Transactions ensure data integrity by guaranteeing that either all operations complete successfully or none of them take effect. This is crucial for maintaining consistency in scenarios like:

- Financial operations (transfers, payments)
- Multi-table updates that must remain synchronized
- Complex data modifications that span multiple records
- Any operation where partial completion would leave data in an inconsistent state

## Basic Usage

### Creating a Transaction

Use `BeginTransactionAsync()` to start a new transaction:

```csharp
using var transaction = await client.BeginTransactionAsync();

// Execute operations...

await transaction.CommitAsync();
```

### Committing Changes

Call `CommitAsync()` to make all changes permanent:

```csharp
using var transaction = await client.BeginTransactionAsync();

await transaction.ExecuteAsync("INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "John Doe", email = "john@example.com" });

await transaction.ExecuteAsync("INSERT INTO audit_log (action, timestamp) VALUES (@action, @timestamp)",
    new { action = "User created", timestamp = DateTime.UtcNow });

// Commit both operations atomically
await transaction.CommitAsync();
```

### Rolling Back Changes

Call `RollbackAsync()` to discard all changes:

```csharp
using var transaction = await client.BeginTransactionAsync();

try
{
    await transaction.ExecuteAsync("INSERT INTO accounts (name, balance) VALUES (@name, @balance)",
        new { name = "Account A", balance = 1000 });

    // Some validation fails
    if (someCondition)
    {
        await transaction.RollbackAsync();
        return;
    }

    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    throw;
}
```

## Auto-Rollback on Dispose

Transactions automatically roll back if disposed without being committed:

```csharp
using (var transaction = await client.BeginTransactionAsync())
{
    await transaction.ExecuteAsync("INSERT INTO users (name) VALUES ('Test')");
    
    // If an exception occurs or we return early, the transaction
    // automatically rolls back when disposed
    if (errorCondition)
        return; // Automatic rollback
    
    await transaction.CommitAsync();
} // Also auto-rolls back here if not committed
```

## Transaction State

Check if a transaction is still active using the `IsActive` property:

```csharp
var transaction = await client.BeginTransactionAsync();

Console.WriteLine(transaction.IsActive); // true

await transaction.CommitAsync();

Console.WriteLine(transaction.IsActive); // false
```

## Examples

### Financial Transfer

```csharp
public async Task TransferMoneyAsync(int fromAccountId, int toAccountId, decimal amount)
{
    using var transaction = await _client.BeginTransactionAsync();

    try
    {
        // Withdraw from source account
        await transaction.ExecuteAsync(
            "UPDATE accounts SET balance = balance - @amount WHERE id = @id",
            new { amount, id = fromAccountId });

        // Deposit to destination account
        await transaction.ExecuteAsync(
            "UPDATE accounts SET balance = balance + @amount WHERE id = @id",
            new { amount, id = toAccountId });

        // Record the transfer
        await transaction.ExecuteAsync(
            "INSERT INTO transfers (from_account, to_account, amount, timestamp) VALUES (@from, @to, @amount, @timestamp)",
            new { from = fromAccountId, to = toAccountId, amount, timestamp = DateTime.UtcNow });

        // All operations succeed or fail together
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Multi-Table Update

```csharp
public async Task UpdateUserAndProfileAsync(int userId, string name, string bio)
{
    using var transaction = await _client.BeginTransactionAsync();

    // Update user
    await transaction.ExecuteAsync(
        "UPDATE users SET name = @name WHERE id = @id",
        new { name, id = userId });

    // Update profile
    await transaction.ExecuteAsync(
        "UPDATE profiles SET bio = @bio WHERE user_id = @userId",
        new { bio, userId });

    // Commit both updates atomically
    await transaction.CommitAsync();
}
```

### Complex Data Migration

```csharp
public async Task MigrateOrdersAsync(int customerId)
{
    using var transaction = await _client.BeginTransactionAsync();

    try
    {
        // Get orders to migrate
        var ordersResult = await transaction.QueryAsync(
            "SELECT * FROM old_orders WHERE customer_id = @id",
            new { id = customerId });

        // Insert into new table
        foreach (var order in ordersResult.Results)
        {
            await transaction.ExecuteAsync(
                "INSERT INTO new_orders (id, customer_id, total, date) VALUES (@id, @customerId, @total, @date)",
                new
                {
                    id = order["id"],
                    customerId = order["customer_id"],
                    total = order["total"],
                    date = order["order_date"]
                });
        }

        // Delete from old table
        await transaction.ExecuteAsync(
            "DELETE FROM old_orders WHERE customer_id = @id",
            new { id = customerId });

        // Mark migration complete
        await transaction.ExecuteAsync(
            "INSERT INTO migration_log (customer_id, migrated_at) VALUES (@id, @timestamp)",
            new { id = customerId, timestamp = DateTime.UtcNow });

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

## Best Practices

### 1. Always Use Using Statements

Wrap transactions in `using` statements to ensure proper disposal:

```csharp
// ✅ Good
using var transaction = await client.BeginTransactionAsync();
// ...

// ❌ Avoid
var transaction = await client.BeginTransactionAsync();
// Might not dispose properly
```

### 2. Keep Transactions Short

Minimize the time a transaction is open:

```csharp
// ✅ Good - Quick transaction
using var transaction = await client.BeginTransactionAsync();
await transaction.ExecuteAsync("UPDATE accounts SET balance = @balance WHERE id = @id", data);
await transaction.CommitAsync();

// ❌ Avoid - Long-running transaction
using var transaction = await client.BeginTransactionAsync();
await SomeExpensiveOperationAsync(); // Don't do this!
await transaction.ExecuteAsync("...");
await transaction.CommitAsync();
```

### 3. Handle Exceptions Properly

Always handle exceptions and roll back when needed:

```csharp
using var transaction = await client.BeginTransactionAsync();

try
{
    // Operations...
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    // Log error, notify user, etc.
    throw;
}
```

### 4. Don't Commit or Rollback Multiple Times

Once committed or rolled back, a transaction cannot be reused:

```csharp
var transaction = await client.BeginTransactionAsync();

await transaction.CommitAsync();

// ❌ This will throw InvalidOperationException
await transaction.ExecuteAsync("...");
```

### 5. Check IsActive Before Operations

When working with long-lived transaction references:

```csharp
if (transaction.IsActive)
{
    await transaction.CommitAsync();
}
```

## Implementation Details

### Atomic Execution

CloudflareD1.NET transactions use D1's batch API under the hood. All operations are collected and executed atomically when `CommitAsync()` is called:

```csharp
// Operations are queued
await transaction.ExecuteAsync("INSERT ...");
await transaction.ExecuteAsync("UPDATE ...");

// All operations execute atomically here
await transaction.CommitAsync();
```

### Deferred Execution

Operations return placeholder results immediately. Actual execution happens on commit:

```csharp
// Returns immediately with placeholder result
var result = await transaction.ExecuteAsync("INSERT ...");

// Actual execution happens here
await transaction.CommitAsync();
```

### Error Handling

If any operation in the batch fails, the entire transaction is rolled back:

```csharp
using var transaction = await client.BeginTransactionAsync();

await transaction.ExecuteAsync("INSERT INTO users ...");
await transaction.ExecuteAsync("INVALID SQL"); // This will fail

// CommitAsync will throw, and no changes will be applied
await transaction.CommitAsync(); // Throws exception, rolls back all
```

## API Reference

### ITransaction Interface

```csharp
public interface ITransaction : IAsyncDisposable
{
    bool IsActive { get; }
    Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

#### Methods

- **QueryAsync** - Execute a SELECT query within the transaction
- **ExecuteAsync** - Execute INSERT, UPDATE, DELETE within the transaction
- **CommitAsync** - Commit all operations atomically
- **RollbackAsync** - Discard all operations

#### Properties

- **IsActive** - Returns `true` if the transaction hasn't been committed or rolled back

## Limitations

### Local vs Remote Mode

Transactions work in both local (SQLite) and remote (Cloudflare D1) modes.

### Nested Transactions

Nested transactions are not supported. Create a new transaction after the previous one is completed:

```csharp
// ✅ Sequential transactions
using (var transaction1 = await client.BeginTransactionAsync())
{
    // ...
    await transaction1.CommitAsync();
}

using (var transaction2 = await client.BeginTransactionAsync())
{
    // ...
    await transaction2.CommitAsync();
}
```

### Isolation Levels

CloudflareD1.NET uses D1's default isolation level (typically Read Committed). Custom isolation levels are not supported.

## See Also

- [Batch Operations](./batch-operations.md) - Efficient bulk operations
- [Getting Started](./getting-started/overview.md) - Initial setup
- [LINQ Queries](./linq/overview.md) - Type-safe queries
