# Code-First Migration Generation Example

This example demonstrates how to automatically generate migrations from your Code-First model changes using the `dotnet d1 migrations add --code-first` command.

## Overview

Instead of manually writing migrations, you can:
1. Define or modify your entity classes
2. Run `dotnet d1 migrations add --code-first` 
3. The tool automatically detects changes and generates the migration

## Prerequisites

```bash
# Install the CLI tool
dotnet tool install -g dotnet-d1

# Or update if already installed
dotnet tool update -g dotnet-d1
```

## Quick Start

### 1. Define Your Models

Create your entity classes with attributes:

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
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    [Column("total")]
    public decimal Total { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
```

### 2. Create Your DbContext

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;

public class MyDbContext : D1Context
{
    public MyDbContext(D1Client client) : base(client) { }

    public D1Set<User> Users { get; set; } = null!;
    public D1Set<Order> Orders { get; set; } = null!;

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

### 3. Build Your Project

```bash
dotnet build --configuration Release
```

### 4. Generate Initial Migration

```bash
dotnet d1 migrations add InitialCreate \
    --code-first \
    --context MyApp.Data.MyDbContext \
    --assembly bin/Release/net8.0/MyApp.dll \
    --connection .migrations.db
```

**Output:**
```
🧮 Generating Code-First migration...

Loading context: MyApp.Data.MyDbContext...
✓ Discovered 2 entity type(s)
Pending model changes:

  + Create table 'users' with 4 column(s)
  + Create table 'orders' with 4 column(s)

✓ Created migration: 20251028120000_InitialCreate.cs
  Location: Migrations/20251028120000_InitialCreate.cs

Next steps:
  1. Review the generated migration
  2. Run 'dotnet d1 database update' to apply it
```

### 5. Review Generated Migration

The tool creates a migration file like:

```csharp
using CloudflareD1.NET.Migrations;

namespace MyApp.Migrations;

public class Migration_20251028120000_InitialCreate : Migration
{
    public override string Id => "20251028120000";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("username").NotNull();
            t.Text("email");
            t.Text("created_at");
        });

        builder.CreateTable("orders", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("order_number").NotNull();
            t.Integer("user_id");
            t.Real("total");
            t.ForeignKey("user_id", "users", "id");
        });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("orders");
        builder.DropTable("users");
    }
}
```

### 6. Apply Migration

```bash
dotnet d1 database update
```

## Making Changes

### 1. Modify Your Model

Add a new property to User:

```csharp
[Table("users")]
public class User
{
    // ... existing properties ...

    [Column("is_active")]
    public bool IsActive { get; set; } // New property
}
```

### 2. Generate Update Migration

```bash
dotnet d1 migrations add AddUserIsActive \
    --code-first \
    --context MyApp.Data.MyDbContext \
    --assembly bin/Release/net8.0/MyApp.dll
```

**Output:**
```
🧮 Generating Code-First migration...

Loading context: MyApp.Data.MyDbContext...
Pending model changes:

  + Add column 'users.is_active' (INTEGER)

✓ Created migration: 20251028130000_AddUserIsActive.cs
```

### 3. Apply Changes

```bash
dotnet d1 database update
```

## Checking for Pending Changes

To see what changes would be generated without creating a migration:

```csharp
using CloudflareD1.NET.CodeFirst.MigrationGeneration;

var generator = new CodeFirstMigrationGenerator(client);
var changesSummary = await generator.GetChangesSummaryAsync(context);
Console.WriteLine(changesSummary);
```

Or check programmatically:

```csharp
var hasPending = await generator.HasPendingChangesAsync(context);
if (hasPending)
{
    Console.WriteLine("⚠️  You have pending model changes. Run migrations add to create a migration.");
}
```

## Command Options

### Required Options

- `--context`: Fully qualified DbContext type name
  - Example: `MyApp.Data.MyDbContext`
  - Must inherit from `D1Context`

- `--assembly`: Path to compiled assembly
  - Example: `bin/Release/net8.0/MyApp.dll`
  - Must be a compiled .NET assembly

### Optional Options

- `--connection`: SQLite database path for schema introspection
  - Default: `.migrations.db`
  - Used to compare current schema with model
  - Can be `:memory:` for a new database

## Best Practices

1. **Always build before generating migrations**
   ```bash
   dotnet build --configuration Release
   ```

2. **Use meaningful migration names**
   ```bash
   # Good
   dotnet d1 migrations add AddUserEmailIndex --code-first ...
   
   # Bad
   dotnet d1 migrations add Update1 --code-first ...
   ```

3. **Review generated migrations before applying**
   - Check the Up() and Down() methods
   - Ensure data migrations are handled if needed
   - Add custom SQL if required

4. **Test rollback**
   ```bash
   dotnet d1 database update
   dotnet d1 database rollback
   ```

5. **Version control your migrations**
   - Commit migration files to source control
   - Never modify applied migrations
   - Create new migrations for changes

## Troubleshooting

### "Could not find context type"

Ensure the `--context` value is the fully qualified type name including namespace:

```bash
--context MyApp.Data.MyDbContext  # ✓ Correct
--context MyDbContext              # ✗ Wrong
```

### "Assembly not found"

Ensure the assembly path is relative to your current directory:

```bash
--assembly ./bin/Release/net8.0/MyApp.dll  # If in project root
--assembly bin/Release/net8.0/MyApp.dll    # Also valid
```

### "No changes detected"

This means your model matches the current database schema. Make changes to your entity classes and rebuild.

## Advanced Usage

### Programmatic Generation

You can also generate migrations programmatically:

```csharp
using CloudflareD1.NET.CodeFirst.MigrationGeneration;

var generator = new CodeFirstMigrationGenerator(client);
var migrationPath = await generator.GenerateMigrationAsync(
    context, 
    "MyMigrationName", 
    "Migrations"
);

Console.WriteLine($"Generated: {migrationPath}");
```

### Integration with CI/CD

```yaml
# .github/workflows/migrations.yml
- name: Check for pending migrations
  run: |
    dotnet build
    # Add custom check here using the generator API
```

## Comparison with Entity Framework Core

If you're coming from EF Core, here's the equivalent:

| EF Core | CloudflareD1.NET |
|---------|------------------|
| `dotnet ef migrations add MyMigration` | `dotnet d1 migrations add MyMigration --code-first --context ... --assembly ...` |
| `dotnet ef database update` | `dotnet d1 database update` |
| `dotnet ef migrations remove` | `dotnet d1 database rollback` |
| `DbContext` | `D1Context` |
| `DbSet<T>` | `D1Set<T>` |

## Next Steps

- [Code-First Documentation](../docs/code-first.md)
- [Migrations Guide](../docs/migrations.md)
- [CLI Reference](../tools/dotnet-d1/README.md)
