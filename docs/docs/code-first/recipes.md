---
id: recipes
title: Code-First Recipes
sidebar_label: Recipes
---

Practical, copy-paste friendly patterns for common Code‑First scenarios with CloudflareD1.NET.

## Entities and mapping

- Table name: decorate the CLR type with `[Table("table_name")]`.
- Column name: decorate the property with `[Column("column_name")]`.
- Primary key: mark with `[Key]`.
- Required column: mark with `[Required]`.
- Ignore a property: mark with `[NotMapped]`.

Example:

```csharp
using CloudflareD1.NET.CodeFirst.Attributes;

[Table("customers")]
public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("full_name")]
    public string Name { get; set; } = string.Empty;

    // Not persisted
    [NotMapped]
    public string? Shadow { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

[Table("orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key column
    public int CustomerId { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
}
```

## Relationships (fluent API)

Configure relationships in your `D1Context.OnModelCreating` method using the fluent API:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>(b =>
    {
        // Customer 1..* Order
        b.HasMany(c => c.Orders)
         .WithOne(o => o.Customer)
         .HasForeignKey(o => o.CustomerId)
         .OnDelete(DeleteBehavior.Cascade); // Cascade delete orders when a customer is deleted
    });
}
```

Supported relationship options:

- HasOne / HasMany + WithOne / WithMany
- HasForeignKey / HasPrincipalKey
- OnDelete(DeleteBehavior.NoAction | Cascade | SetNull | Restrict)
- IsRequired() to enforce non-null FKs

## Indexes

Define indexes via attribute or fluent API (including composite and unique indexes).

Attribute-based:

```csharp
using CloudflareD1.NET.CodeFirst.Attributes;

[Index(nameof(Customer.Name), IsUnique = true, Name = "ix_customers_name")] // single column
[Table("customers")]
public class Customer { /* ... */ }
```

Fluent API (single or composite):

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>(b =>
    {
        // Composite unique index on (CustomerId, CreatedAt)
        b.HasIndex(o => new { o.CustomerId, o.CreatedAt })
         .IsUnique()
         .HasName("ix_orders_customer_created");
    });
}
```

The diff tooling generates the corresponding CREATE INDEX statements for migrations.

## End-to-end sample

```csharp
public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<Customer> Customers => Set<Customer>();
    public D1Set<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.HasMany(c => c.Orders)
             .WithOne(o => o.Customer)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasIndex(o => new { o.CustomerId, o.CreatedAt })
             .IsUnique()
             .HasName("ix_orders_customer_created");
        });
    }
}
```

## Running diffs from the CLI

The CLI will instantiate your context and invoke `OnModelCreating` when it can construct it with a `D1Client`. Ensure your context exposes a constructor that accepts `D1Client` as shown above.

Typical workflow:

```bash
# From your project directory
# Generate a migration by diffing the current database against your model
 dotnet d1 migrations diff -c AppDbContext -o Migrations

# List migrations (optional)
 dotnet d1 migrations list

# Scaffold SQL or apply as appropriate in your environment
 dotnet d1 migrations scaffold -o Migrations/Out
```

Notes:

- If the CLI can't construct your context, it will fall back to attribute-only discovery (no fluent configuration).
- Indexes and foreign keys defined via fluent API and attributes are included in the diff.
- Delete behaviors map to SQLite as: NoAction, Cascade, SetNull, Restrict.
