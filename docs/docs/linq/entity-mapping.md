---
sidebar_position: 4
---

# Entity Mapping

The LINQ package automatically maps database query results to your strongly-typed C# classes.

## Defining Entities

### Basic Entity

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### With Nullable Properties

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }  // Nullable
    public string? PhoneNumber { get; set; }  // Nullable
    public DateTime? LastLoginAt { get; set; }  // Nullable DateTime
}
```

### Complex Types

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductStatus Status { get; set; }  // Enum
    public bool IsActive { get; set; }  // Boolean
    public DateTime CreatedAt { get; set; }  // DateTime
    public Guid TrackingId { get; set; }  // Guid
    public int? CategoryId { get; set; }  // Nullable int
}

public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}
```

## Column Name Mapping

### Snake_case to PascalCase

The default mapper automatically converts snake_case database columns to PascalCase properties:

| Database Column | C# Property |
|----------------|-------------|
| `user_id` | `UserId` |
| `email_address` | `EmailAddress` |
| `created_at` | `CreatedAt` |
| `is_active` | `IsActive` |
| `first_name` | `FirstName` |

```csharp
// Database: user_id, full_name, email_address, created_at
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### Exact Matching

If property names match column names exactly, mapping works automatically:

```csharp
// Database: id, name, email
public class User
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
}
```

## Type Conversions

### Supported Types

The default mapper handles automatic conversion for:

#### Primitives
- `int`, `long`, `short`, `byte`
- `decimal`, `float`, `double`
- `bool`
- `string`

#### Special Types
- `DateTime` - Parsed from strings or Unix timestamps
- `Guid` - Parsed from string representations
- `Enum` - Mapped from integers or strings

#### Nullable Types
All of the above with `?` suffix:
- `int?`, `decimal?`, `DateTime?`, etc.

### SQLite Boolean Handling

SQLite stores booleans as 0 or 1. The mapper automatically converts:

```csharp
// SQLite: 0 → false, 1 → true
public class User
{
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public bool? IsSubscribed { get; set; }  // Nullable
}
```

### DateTime Handling

DateTime values are parsed from multiple formats:

```csharp
public class Event
{
    // Handles: ISO 8601, RFC 3339, Unix timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### Enum Handling

Enums can be stored as integers or strings:

```csharp
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3
}

public class Order
{
    public OrderStatus Status { get; set; }  // Maps from int or string
}
```

## Custom Mappers

### Implementing IEntityMapper

Create custom mapping logic by implementing `IEntityMapper`:

```csharp
using CloudflareD1.NET.Linq.Mapping;

public class CustomUserMapper : IEntityMapper
{
    public T Map<T>(Dictionary<string, object?> row)
    {
        if (typeof(T) == typeof(User))
        {
            var user = new User
            {
                // Custom column names
                Id = Convert.ToInt32(row["user_id"]),
                FullName = row["full_name"]?.ToString() ?? "",
                Email = row["email_address"]?.ToString() ?? "",
                
                // Custom logic
                IsActive = row["status"]?.ToString() == "active",
                
                // Transform data
                DisplayName = row["full_name"]?.ToString()?.ToUpper() ?? ""
            };
            return (T)(object)user;
        }
        
        throw new NotSupportedException($"Type {typeof(T)} not supported");
    }

    public IEnumerable<T> MapMany<T>(IEnumerable<Dictionary<string, object?>> rows)
    {
        return rows.Select(Map<T>);
    }
}
```

### Using Custom Mappers

#### With Direct Queries

```csharp
var mapper = new CustomUserMapper();

var users = await client.QueryAsync<User>(
    "SELECT * FROM users",
    parameters: null,
    mapper: mapper
);
```

#### With Query Builder

```csharp
var mapper = new CustomUserMapper();

var users = await client.Query<User>("users", mapper)
    .Where("is_active = ?", true)
    .ToListAsync();
```

## Real-World Examples

### E-Commerce Product

```csharp
public class Product
{
    // Database: product_id
    public int ProductId { get; set; }
    
    // Database: product_name
    public string ProductName { get; set; } = string.Empty;
    
    // Database: sku
    public string Sku { get; set; } = string.Empty;
    
    // Database: price (decimal)
    public decimal Price { get; set; }
    
    // Database: quantity_in_stock
    public int QuantityInStock { get; set; }
    
    // Database: is_available (0 or 1)
    public bool IsAvailable { get; set; }
    
    // Database: category_id (nullable)
    public int? CategoryId { get; set; }
    
    // Database: created_at (ISO string)
    public DateTime CreatedAt { get; set; }
    
    // Database: updated_at (ISO string, nullable)
    public DateTime? UpdatedAt { get; set; }
}

// Query
var products = await client.QueryAsync<Product>(@"
    SELECT product_id, product_name, sku, price, 
           quantity_in_stock, is_available, category_id,
           created_at, updated_at
    FROM products
    WHERE is_available = 1
");
```

### User with Profile

```csharp
public class UserProfile
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPremium { get; set; }
    public UserRole Role { get; set; }
}

public enum UserRole
{
    User = 0,
    Moderator = 1,
    Admin = 2
}

var profile = await client.QueryFirstOrDefaultAsync<UserProfile>(
    "SELECT * FROM users WHERE user_id = @id",
    new { id = 123 }
);
```

### Audit Log Entry

```csharp
public class AuditLog
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid TraceId { get; set; }
}

var recentLogs = await client.Query<AuditLog>("audit_logs")
    .Where("timestamp > ?", DateTime.UtcNow.AddHours(-24))
    .OrderByDescending("timestamp")
    .Take(100)
    .ToListAsync();
```

## Performance

### Reflection Caching

The default mapper uses `ConcurrentDictionary` to cache:
- Property information for each type
- Column-to-property mappings

This means:
- **First query for a type**: ~10-20ms overhead
- **Subsequent queries**: Less than 1ms overhead
- **Mapping 1000 rows**: Less than 1ms total

### Best Practices

1. **Reuse mapper instances**: Create once, use many times
2. **Keep entities simple**: Avoid complex computed properties
3. **Use nullable types appropriately**: Match database schema
4. **Name properties clearly**: Follow PascalCase conventions

## Troubleshooting

### Property Not Mapped

**Problem**: Database column not mapping to property

**Solutions**:
1. Check property name matches column (with snake_case conversion)
2. Ensure property has a public setter
3. Verify column name in database
4. Use custom mapper for non-standard mappings

### Type Conversion Error

**Problem**: Cannot convert database value to property type

**Solutions**:
1. Check database column type matches C# property type
2. Use nullable types for nullable columns
3. Implement custom mapper with explicit conversions

### Missing Properties

**Problem**: Some properties are null after mapping

**Solutions**:
1. Verify column is in SELECT query results
2. Check for typos in column or property names
3. Ensure database has data in those columns

## What's Next?

- **[Query Builder](query-builder)** - Build fluent queries
