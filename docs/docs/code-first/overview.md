---
sidebar_position: 1
---

# Code-First (Models → Migrations)

CloudflareD1.NET.CodeFirst lets you define your database schema using C# classes and attributes, plus an optional fluent API for relationships and indexes. The framework can **automatically generate migrations** from your models by comparing them with the current database schema.

## Quick start

1) Define your entities and a D1Context

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    [AutoIncrement]
    [Column("id")] public int Id { get; set; }

    [Required]
    [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("email")] public string Email { get; set; } = string.Empty;
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<User> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>();
    }
}
```

1b) Use your context to query and persist data

```csharp
var context = new AppDbContext(client);

// Add entities
var user = new User { Name = "Alice", Email = "alice@example.com" };
context.Users.Add(user);
await context.SaveChangesAsync();
// user.Id is now populated

// Update entities
user.Name = "Alice Smith";
context.Users.Update(user);
await context.SaveChangesAsync();

// Delete entities
context.Users.Remove(user);
await context.SaveChangesAsync();

// Query with LINQ
var users = await context.Users
    .AsQueryable()
    .Where("email LIKE ?", "%@example.com")
    .ToListAsync();
```

2) Build your project and generate a migration

```bash
# build your app (produces your dll)
dotnet build --configuration Release

# generate a migration from your Code-First models
# --context: fully qualified context type name
# --assembly: path to compiled dll with your context
# --connection: path to your SQLite database
dotnet d1 migrations add InitialCreate --code-first \
  --context MyApp.Data.AppDbContext \
  --assembly bin/Release/net8.0/MyApp.dll \
  --connection database.db
```

The command generates a timestamped migration file in your project's `Migrations/` folder with `Up()` and `Down()` methods.

3) Apply the migration

```bash
dotnet d1 migrations apply
```

## Automatic Migration Generation

CloudflareD1.NET can **automatically detect changes** between your Code-First models and the current database schema, then generate the appropriate migration code.

**How it works:**
1. The CLI loads your `D1Context` and extracts model metadata
2. It introspects the current database schema
3. It compares the two and detects differences
4. It generates a timestamped migration with SQL operations

**Supported change detection:**
- ✅ New tables
- ✅ Dropped tables
- ✅ New columns
- ✅ Dropped columns
- ✅ New indexes
- ✅ Dropped indexes
- ✅ New foreign keys
- ✅ Dropped foreign keys

See the [Migration Generation](./migration-generation.md) guide for detailed usage and examples.

## Attributes

- Table/Column
  - `[Table("users")]` — custom table name
  - `[Column("email")]` — custom column name/type
- Keys/Nullability
  - `[Key]` — primary key
  - `[Required]` — NOT NULL
- Ignore
  - `[NotMapped]` — skip a property
- Relationships
  - `[ForeignKey(nameof(User))]` — link a FK property to a navigation
- Indexes
  - `[Index(nameof(Email), IsUnique = true, Name = "idx_users_email")]`
  - `[Index(nameof(FirstName), nameof(LastName))]` — composite index

## Fluent API (OnModelCreating)

Use `OnModelCreating(ModelBuilder)` to configure relationships and indexes fluently:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // relationships
    modelBuilder.Entity<Post>()
        .HasOne(p => p.User)
        .WithMany(u => u.Posts)
        .HasForeignKey(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade); // NO ACTION | CASCADE | SET NULL | RESTRICT

    // indexes
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Sku)
        .IsUnique()
        .HasName("idx_unique_sku");

    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Name); // non-unique
}
```

Notes:
- The CLI tries to construct your context with a `D1Client` and read its `Model` property. When successful, `OnModelCreating` runs, and fluent configuration is honored. If construction fails, it falls back to attribute discovery only.
- Composite index names default to `ix_{table}_{col1}_{col2}` unless you provide `.HasName(...)`.

## Conventions

- Tables default to pluralized snake_case of the class name: `User` → `users`
- Columns default to snake_case of the property name: `CreatedAt` → `created_at`
- Default types: `string → TEXT`, `int → INTEGER`, `DateTime → TEXT`, `bool → INTEGER (0/1)`, `enum → TEXT`
- All properties are nullable unless marked `[Required]`
- Relationships default to `{PrincipalName}Id` as the FK if not specified
- Delete behavior defaults to `NO ACTION` unless configured via `.OnDelete(...)`
- **Navigation properties** (reference types and collections) are ignored by default and not mapped to columns

## Change Tracking & SaveChanges

CloudflareD1.NET.CodeFirst includes automatic change tracking for Add/Update/Remove operations:

```csharp
var context = new AppDbContext(client);

// Track a new entity
var user = new User { Name = "Bob", Email = "bob@example.com" };
context.Users.Add(user);

// Track an update - only changed properties are updated
user.Email = "bob.new@example.com";  // Only Email property changed
context.Users.Update(user);
// Generates: UPDATE users SET email = ? WHERE id = ?

// Track a deletion
context.Users.Remove(user);

// Persist all changes in order (INSERT → UPDATE → DELETE)
int rowsAffected = await context.SaveChangesAsync();
```

### Per-Property Change Detection

`Update` intelligently tracks which properties have changed:

```csharp
// Insert a product
var product = new Product { Name = "Widget", Price = 19.99m, Stock = 100 };
context.Products.Add(product);
await context.SaveChangesAsync();
// product.Id is now populated

// Update only the Price - only that column is updated
product.Price = 24.99m;
context.Products.Update(product);
await context.SaveChangesAsync();
// SQL: UPDATE products SET price = ? WHERE id = ?
// (Name and Stock are NOT in the SET clause)

// No changes - no UPDATE statement generated
context.Products.Update(product);  // No properties modified
await context.SaveChangesAsync();  // Returns 0 rows affected
```

**How it works:**
- When an entity is first saved, a snapshot of all property values is captured
- On subsequent `Update()` calls, current values are compared with the snapshot
- Only properties with different values are included in the UPDATE statement
- If no properties changed, the UPDATE is skipped entirely

**Features:**
- Automatically populates auto-increment primary keys after INSERT
- Executes operations sequentially to satisfy Cloudflare D1 API semantics
- **Per-Property Change Detection**: Only updates columns that changed (via snapshot comparison)
- **Foreign Key-Aware Ordering**: Automatically orders INSERT and DELETE operations based on FK dependencies
- Navigation properties and collections are ignored (not mapped to columns)

### Foreign Key-Aware Operation Ordering

`SaveChangesAsync` automatically analyzes foreign key relationships and orders operations to prevent constraint violations:

```csharp
// Example: Adding related entities in any order
var customer = new Customer { Name = "Acme Corp", Email = "contact@acme.com" };
var order = new Order { OrderNumber = "ORD-001", CustomerId = customer.Id, Total = 99.99m };

// Add in any order - SaveChanges will insert Customer first automatically
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
- **Updates**: No reordering needed (FK values should not change)
- **Circular dependencies**: Throws `InvalidOperationException` if detected

**Notes:**
- Primary keys are required for Update and Delete operations
- **Per-property change detection**: Update only modifies columns that changed since the entity was last saved (via snapshot comparison). If no properties change, no UPDATE statement is generated.

## Workflow

### Initial Setup

1. **Define models** with Code-First attributes
2. **Create DbContext** with `OnModelCreating` configuration
3. **Build** your project (`dotnet build --configuration Release`)
4. **Generate migration**: `dotnet d1 migrations add InitialCreate --code-first ...`
5. **Review** the generated migration file
6. **Apply** migration: `dotnet d1 migrations apply`

### Iterating on Models

After changing your models:

1. **Update** your entity classes or DbContext configuration
2. **Rebuild** your project
3. **Generate new migration**: `dotnet d1 migrations add DescriptiveName --code-first ...`
4. **Review** the changes in the generated file
5. **Apply**: `dotnet d1 migrations apply`

The framework automatically detects what changed and generates only the necessary SQL operations.

See also:
- Migrations overview: [Database Migrations](../migrations/overview.md)
- LINQ usage: [LINQ package](../linq/intro.md)
