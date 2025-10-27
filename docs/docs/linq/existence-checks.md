---
sidebar_position: 9
---

# Existence Checks

Existence check methods allow you to efficiently test whether rows matching specific conditions exist in your database using optimized `EXISTS` and `NOT EXISTS` SQL patterns.

## Overview

**Available Methods:**
- **AnyAsync(predicate)** - Check if any rows match a condition
- **AllAsync(predicate)** - Check if all rows match a condition

Both methods use SQL `EXISTS` patterns for optimal performance, avoiding the need to fetch and count rows.

:::info Version
Existence check methods with predicates were introduced in **v1.8.0**.
:::

## AnyAsync - Check for Matching Rows

Returns `true` if **at least one** row matches the specified condition.

### Basic Usage

```csharp
// Check if any users are over 35
var hasOldUsers = await client.Query<User>("users")
    .AnyAsync(u => u.Age > 35);

if (hasOldUsers)
{
    Console.WriteLine("Found users over 35");
}
```

**Generated SQL:**
```sql
SELECT EXISTS(SELECT 1 FROM users WHERE Age > ?)
```

### With Existing Filters

Combines with existing `Where()` clauses:

```csharp
// Check if any active users are under 25
var hasYoungActive = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .AnyAsync(u => u.Age < 25);
```

**Generated SQL:**
```sql
SELECT EXISTS(
    SELECT 1 FROM users 
    WHERE IsActive = ? AND Age < ?
)
```

### Complex Predicates

```csharp
// Check if any users match multiple conditions
var hasPremiumOrYoung = await client.Query<User>("users")
    .AnyAsync(u => u.IsPremium || u.Age < 25);

// With compound conditions
var hasSpecialUsers = await client.Query<User>("users")
    .AnyAsync(u => u.Age > 25 && u.IsActive && u.Email != null);
```

## AllAsync - Check All Rows Match

Returns `true` if **all** rows match the specified condition. Uses `NOT EXISTS` with a negated predicate for efficiency.

### Basic Usage

```csharp
// Check if all users are adults (over 18)
var allAdults = await client.Query<User>("users")
    .AllAsync(u => u.Age > 18);

if (allAdults)
{
    Console.WriteLine("All users are adults");
}
```

**Generated SQL:**
```sql
SELECT NOT EXISTS(
    SELECT 1 FROM users 
    WHERE NOT (Age > ?)
)
```

### With Existing Filters

```csharp
// Check if all active users are verified
var allActiveVerified = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .AllAsync(u => u.IsVerified);
```

**Generated SQL:**
```sql
SELECT NOT EXISTS(
    SELECT 1 FROM users 
    WHERE IsActive = ? AND NOT (IsVerified = ?)
)
```

### Complex Predicates

```csharp
// Check if all users meet complex criteria
var allMeetCriteria = await client.Query<User>("users")
    .AllAsync(u => u.Age >= 18 && u.Email != null);

// With OR conditions
var allActiveOrPremium = await client.Query<User>("users")
    .AllAsync(u => u.IsActive || u.IsPremium);
```

## Performance Benefits

### Why EXISTS is Fast

The `EXISTS` pattern is optimized because:
1. **Stops at first match** - Doesn't need to scan all rows
2. **No data transfer** - Only returns true/false
3. **Index-friendly** - Can use indexes efficiently

```csharp
// ✅ Efficient - Uses EXISTS, stops at first match
var hasOldUsers = await client.Query<User>("users")
    .AnyAsync(u => u.Age > 35);

// ❌ Inefficient - Fetches all matching rows
var users = await client.Query<User>("users")
    .Where(u => u.Age > 35)
    .ToListAsync();
var hasOldUsers = users.Any(); // Wasteful if you only need true/false
```

### Comparison with Count

```csharp
// ✅ Fast - EXISTS stops at first match
var exists = await query.AnyAsync(u => u.Age > 35);

// ❌ Slower - COUNT scans all matching rows
var count = await query.Where(u => u.Age > 35).CountAsync();
var exists = count > 0;
```

## Common Patterns

### Validation Checks

```csharp
// Validate username isn't taken
var usernameTaken = await client.Query<User>("users")
    .AnyAsync(u => u.Username == newUsername);

if (usernameTaken)
{
    throw new ValidationException("Username already exists");
}
```

### Conditional Logic

```csharp
// Check prerequisites before processing
var hasPendingOrders = await client.Query<Order>("orders")
    .Where(o => o.UserId == userId)
    .AnyAsync(o => o.Status == "Pending");

if (hasPendingOrders)
{
    await ProcessPendingOrders(userId);
}
```

### Data Integrity Checks

```csharp
// Ensure all required fields are populated
var allHaveEmail = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .AllAsync(u => u.Email != null && u.Email != "");

if (!allHaveEmail)
{
    await SendEmailReminderToAdmins();
}
```

### Business Rule Validation

```csharp
// Check if all orders are fulfilled
var allOrdersFulfilled = await client.Query<Order>("orders")
    .Where(o => o.CustomerId == customerId)
    .AllAsync(o => o.Status == "Fulfilled");

if (allOrdersFulfilled)
{
    await SendCompletionEmail(customerId);
}
```

## Expression Support

### Comparison Operators

```csharp
// Greater than / Less than
await query.AnyAsync(u => u.Age > 25);
await query.AnyAsync(u => u.Age >= 18);
await query.AnyAsync(u => u.Age < 65);
await query.AnyAsync(u => u.Age <= 100);

// Equality
await query.AnyAsync(u => u.Status == "Active");
await query.AnyAsync(u => u.Id != excludedId);
```

### Logical Operators

```csharp
// AND conditions
await query.AnyAsync(u => u.Age > 18 && u.IsVerified);

// OR conditions
await query.AnyAsync(u => u.IsPremium || u.Age < 25);

// NOT conditions
await query.AllAsync(u => !u.IsBlocked);

// Complex combinations
await query.AnyAsync(u => 
    (u.Age > 25 && u.IsActive) || 
    (u.IsPremium && u.IsVerified));
```

### String Operations

```csharp
// String equality
await query.AnyAsync(u => u.Name == "John");

// String methods (when supported by expression visitor)
await query.AnyAsync(u => u.Email.Contains("@example.com"));
await query.AnyAsync(u => u.Name.StartsWith("A"));
await query.AnyAsync(u => u.Email.EndsWith(".com"));
```

### Null Checks

```csharp
// Check for null
await query.AnyAsync(u => u.Email != null);
await query.AllAsync(u => u.ProfilePictureUrl != null);

// Nullable value types
await query.AnyAsync(u => u.DeletedAt == null); // Not deleted
await query.AnyAsync(u => u.LastLoginAt != null); // Has logged in
```

## Best Practices

### 1. Use AnyAsync Instead of Count > 0

```csharp
// ✅ Good - Stops at first match
var exists = await query.AnyAsync(u => u.Age > 35);

// ❌ Bad - Counts all matching rows
var count = await query.Where(u => u.Age > 35).CountAsync();
var exists = count > 0;
```

### 2. Combine Filters for Efficiency

```csharp
// ✅ Good - Single efficient query
var hasMatch = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .Where(u => u.Country == "US")
    .AnyAsync(u => u.Age > 35);

// ❌ Less efficient - Multiple queries or in-memory filtering
var activeUS = await client.Query<User>("users")
    .Where(u => u.IsActive && u.Country == "US")
    .ToListAsync();
var hasMatch = activeUS.Any(u => u.Age > 35);
```

### 3. Use AllAsync for Validation

```csharp
// ✅ Good - Efficient validation
var allValid = await query.AllAsync(u => u.Email != null);

if (!allValid)
{
    throw new ValidationException("All users must have email");
}
```

### 4. Early Return Pattern

```csharp
// ✅ Good - Early return saves processing
public async Task<bool> CanProcessOrder(int orderId)
{
    // Quick checks first
    var orderExists = await orders.AnyAsync(o => o.Id == orderId);
    if (!orderExists) return false;

    var hasInventory = await inventory.AnyAsync(i => i.OrderId == orderId);
    if (!hasInventory) return false;

    // ... more checks
    return true;
}
```

## Difference from Parameterless AnyAsync()

CloudflareD1.NET has two versions of `AnyAsync()`:

```csharp
// Without predicate - checks if query has any results
var hasUsers = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .AnyAsync(); // Returns true if any active users exist

// With predicate - adds additional condition (v1.8.0+)
var hasOldUsers = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .AnyAsync(u => u.Age > 35); // Returns true if any active users over 35
```

Both are efficient and use optimized SQL patterns.

## Error Handling

```csharp
try
{
    var hasMatch = await client.Query<User>("users")
        .AnyAsync(u => u.Age > 35);
}
catch (ArgumentNullException ex)
{
    // Predicate cannot be null
    Console.WriteLine("Invalid predicate");
}
catch (D1QueryException ex)
{
    // Database query error
    Console.WriteLine($"Query failed: {ex.Message}");
}
```

## See Also

- [Query Builder](./query-builder.md) - Basic query operations
- [Set Operations](./set-operations.md) - Union, Intersect, Except
- [Expression Trees](./expression-trees.md) - Understanding expression translation
