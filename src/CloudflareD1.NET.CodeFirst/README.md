# CloudflareD1.NET.CodeFirst

Code-First ORM for CloudflareD1.NET. Define your database schema using C# classes and attributes, similar to Entity Framework Core.

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.CodeFirst.svg)](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CloudflareD1.NET.CodeFirst.svg)](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)

## Features

- **Entity Attributes**: Define tables, columns, keys, and relationships using attributes
- **DbContext Pattern**: Familiar API for developers coming from Entity Framework
- **Type-Safe Queries**: LINQ support through integration with CloudflareD1.NET.Linq
- **Migration Generation**: Generate migrations from your model classes (coming soon)
- **Fluent API**: Configure entities using the fluent configuration API

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
    [ForeignKey("User")]
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

    // Optional: Configure entities with fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .ToTable("users")
            .HasKey(u => u.Id);

        modelBuilder.Entity<Order>()
            .ToTable("orders")
            .HasKey(o => o.Id);
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

## Running Migrations

Apply migrations to your database:

```csharp
var context = new MyDbContext(client);
var appliedMigrations = await context.MigrateAsync();

foreach (var migration in appliedMigrations)
{
    Console.WriteLine($"Applied migration: {migration}");
}
```

## Related Packages

- **[CloudflareD1.NET](https://www.nuget.org/packages/CloudflareD1.NET/)**: Core client library
- **[CloudflareD1.NET.Linq](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)**: LINQ query support
- **[CloudflareD1.NET.Migrations](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)**: Migration system

## Roadmap

- ✅ Entity attributes (Table, Column, Key, ForeignKey, Required, NotMapped)
- ✅ D1Context base class
- ✅ D1Set entity collections
- ✅ ModelBuilder and metadata system
- ✅ Fluent configuration API
- ⏳ Code-first migration generation
- ⏳ Relationship configuration (one-to-many, many-to-one)
- ⏳ Index configuration
- ⏳ Data annotations validation

## License

MIT License - see LICENSE file for details
