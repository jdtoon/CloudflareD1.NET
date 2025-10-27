---
sidebar_position: 1
---

# Code-First (Models → Migrations)

CloudflareD1.NET.CodeFirst lets you define your database schema using C# classes and attributes, plus an optional fluent API for relationships and indexes. The CLI can diff your model against a saved snapshot and generate migrations.

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
    [Column("id")] public int Id { get; set; }

    [Required]
    [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("email")] public string Email { get; set; } = string.Empty;
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<User> Users { get; set; } = null!;
}
```

2) Build your project and run the CLI diff

```bash
# build your app (produces your dll)
dotnet build

# generate a migration from your model
# --context: fully qualified context type name
# --assembly: path to compiled dll with your context
 dotnet d1 migrations diff InitialFromModel \
  --context MyApp.Data.AppDbContext \
  --assembly bin/Debug/net8.0/MyApp.dll
```

The command saves a migration to your project's `Migrations/` folder and a `.migrations-snapshot.json` used for subsequent diffs.

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
- Default types: `string → TEXT`, `int → INTEGER`, `DateTime → TEXT`, `bool → INTEGER (0/1)`
- All properties are nullable unless marked `[Required]`
- Relationships default to `{PrincipalName}Id` as the FK if not specified
- Delete behavior defaults to `NO ACTION` unless configured via `.OnDelete(...)`

## CLI Diff Workflow

- First run creates both a migration and `.migrations-snapshot.json`
- Later runs diff your model vs snapshot and generate only the changes
- The snapshot is updated after each diff so you can iterate safely

See also:
- Migrations overview: [Database Migrations](../migrations/overview.md)
- LINQ usage: [LINQ package](../linq/intro.md)
