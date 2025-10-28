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

// Update
user.Email = "new@example.com";
context.Users.Update(user);
await context.SaveChangesAsync();

// Delete
context.Users.Remove(user);
await context.SaveChangesAsync();
```

Notes:
- Primary keys are required for updates and deletes.
- For auto-increment keys, if you don't set the key before insert, it will be populated from the database.
- Current implementation updates all non-key columns for `Update` (no per-property change detection yet).
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
- ⏳ Index configuration helpers
- ⏳ Data annotations validation

## License

MIT License - see LICENSE file for details
