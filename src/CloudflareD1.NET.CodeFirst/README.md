# CloudflareD1.NET.CodeFirst

Code-First ORM for CloudflareD1.NET. Define your database schema using C# classes and attributes, similar to Entity Framework Core.

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.CodeFirst.svg)](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CloudflareD1.NET.CodeFirst.svg)](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)

## Features

- **Entity Attributes**: Define tables, columns, keys, and relationships using attributes
- **DbContext Pattern**: Familiar API for developers coming from Entity Framework
- **Type-Safe Queries**: LINQ support through integration with CloudflareD1.NET.Linq
- **Automatic Migration Generation**: Generate migrations from your model classes with `dotnet d1 migrations add --code-first` (snapshot-based, no DB required at generation time)
- **Fluent API**: Configure entities using the fluent configuration API
- **Snapshot-Based Diffs**: Compare your model with the last saved migration snapshot to detect changes

## Installation

```bash
dotnet add package CloudflareD1.NET.CodeFirst
```

## Quick Start

### Define Your Entities

```csharp
using CloudflareD1.NET.CodeFirst.Attributes;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public List<Order> Orders { get; set; } = new();
}

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("user_id")]
    public int UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
```

### Create a DbContext

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;

public class MyDbContext : D1Context
{
    public MyDbContext(D1Client client) : base(client)
    {
    }

    // Entity sets
    public D1Set<User> Users { get; set; } = null!;
    public D1Set<Order> Orders { get; set; } = null!;

    // Recommended: Configure relationships with Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .ToTable("users")
            .HasKey(u => u.Id);

        modelBuilder.Entity<Order>()
            .ToTable("orders")
            .HasKey(o => o.Id)
            .HasForeignKey(o => o.UserId);
    }
}
```

### Use the DbContext

```csharp
// Initialize D1Client
var options = new D1Options
{
    AccountId = "your-account-id",
    DatabaseId = "your-database-id",
    ApiToken = "your-api-token"
};
var client = new D1Client(options);

// Create context
var context = new MyDbContext(client);

// Query entities using LINQ
var users = await context.Users
    .AsQueryable()
    .Where("username LIKE ?", "john%")
    .ToListAsync();

// Find by primary key
var user = await context.Users.FindAsync(1);

// Get all orders
var allOrders = await context.Orders.ToListAsync();
```

### Change tracking and SaveChanges

You can add, update, and remove entities and persist changes with `SaveChangesAsync`:

```csharp
// Insert
var user = new User { Username = "john_doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow };
context.Users.Add(user);
await context.SaveChangesAsync();
// user.Id is populated if it's an auto-increment key

// Update - only changed properties
user.Email = "new@example.com";  // Only Email changed
context.Users.Update(user);
await context.SaveChangesAsync();  // Generates: UPDATE users SET email = ? WHERE id = ?

// Update multiple properties
user.Email = "another@example.com";
user.Username = "jane_doe";
context.Users.Update(user);
await context.SaveChangesAsync();  // Generates: UPDATE users SET email = ?, username = ? WHERE id = ?

// No changes - no UPDATE generated
context.Users.Update(user);  // No properties modified
await context.SaveChangesAsync();  // 0 rows affected, no SQL executed

// Delete
context.Users.Remove(user);
await context.SaveChangesAsync();
```

**Per-Property Change Detection**: `Update` intelligently detects which properties have changed since the entity was last saved (via snapshot comparison). Only changed columns are included in the UPDATE statement, improving performance and reducing unnecessary writes.

#### Foreign Key-Aware Operation Ordering

`SaveChangesAsync` automatically orders INSERT and DELETE operations based on foreign key dependencies to prevent constraint violations:

```csharp
// Example: Adding related entities in any order
var customer = new Customer { Name = "Acme Corp", Email = "contact@acme.com" };
var order = new Order { OrderNumber = "ORD-001", CustomerId = customer.Id, Total = 99.99m };

// Add in any order - SaveChanges will insert Customer first
context.Orders.Add(order);
context.Customers.Add(customer);  // Added second, but will be inserted first!
await context.SaveChangesAsync();  // Customer → Order (correct FK order)

// Deleting also respects FK constraints
context.Customers.Remove(customer);  // Added first
context.Orders.Remove(order);        // Added second
await context.SaveChangesAsync();    // Order → Customer (deletes child first)
```

**How it works:**
- **Inserts**: Parent entities (referenced by FKs) are inserted before children
- **Deletes**: Child entities (with FKs) are deleted before parents
- **Updates**: No reordering (FK values should not change during updates)
- **Circular dependencies**: If detected, an `InvalidOperationException` is thrown

Notes:
- Primary keys are required for updates and deletes.
- For auto-increment keys, if you don't set the key before insert, it will be populated from the database.
- **Per-property change detection**: `Update` only modifies columns that have changed since the entity was last saved. If no properties change, no UPDATE statement is generated.
- Insert/Update/Delete are executed sequentially when you call `SaveChangesAsync` (to satisfy the Cloudflare D1 API semantics).

## Attributes

### Table Attributes

- **`[Table("name")]`**: Specifies the database table name for an entity
- **`[NotMapped]`**: Excludes a property from database mapping

### Column Attributes

- **`[Column("name", TypeName="TEXT")]`**: Specifies column name and SQL type
- **`[Key]`**: Marks a property as the primary key
- **`[Required]`**: Makes a column NOT NULL
- **`[ForeignKey("PropertyName")]`**: Defines a foreign key relationship

## Entity Conventions

If you don't use attributes, the framework follows these conventions:

- **Table Names**: Pluralized class name in snake_case (`User` → `users`)
- **Column Names**: Property name in snake_case (`UserId` → `user_id`)
- **Primary Keys**: Properties named `Id` or `{ClassName}Id` become primary keys
- **Types**: C# types map to SQLite types automatically:
  - `int`, `long`, `short`, `byte`, `bool` → `INTEGER`
  - `double`, `float`, `decimal` → `REAL`
  - `string`, `DateTime`, `Guid` → `TEXT`
  - `byte[]` → `BLOB`
 - **Navigation properties & collections**: Reference navigations (e.g., `Order.Customer`) and collections (e.g., `User.Orders`) are ignored by default and are not mapped to columns. You can also explicitly exclude any property with `[NotMapped]`.

## Fluent API

Configure entities programmatically in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .ToTable("users")
        .Property(u => u.Username)
        .IsRequired()
        .HasColumnName("user_name");
}
```

## Generating Migrations (Code-First)

Build your project, then generate a migration from your models:

```bash
dotnet d1 migrations add InitialCreate --code-first \
    --context MyNamespace.MyDbContext \
    --assembly bin/Release/net8.0/MyApp.dll
```

This compares your model against the JSON snapshot at `Migrations/.migrations-snapshot.json` and generates only the delta.

To apply migrations, use the CLI:

```bash
dotnet d1 migrations apply
```

## Related Packages

- **[CloudflareD1.NET](https://www.nuget.org/packages/CloudflareD1.NET/)**: Core client library
- **[CloudflareD1.NET.Linq](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)**: LINQ query support
- **[CloudflareD1.NET.Migrations](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)**: Migration system

## Roadmap

- ✅ Entity attributes (Table, Column, Key, Required, NotMapped)
- ✅ D1Context base class
- ✅ D1Set entity collections
- ✅ ModelBuilder and metadata system
- ✅ Fluent configuration API
- ✅ Code-first migration generation (snapshot-based)
- ✅ Foreign keys via Fluent API
- ✅ Basic change tracking (Add/Update/Remove) and SaveChanges
- ✅ Foreign key-aware operation ordering (automatic INSERT/DELETE ordering)
- ⏳ Index configuration helpers
- ⏳ Data annotations validation

## License

MIT License - see LICENSE file for details
