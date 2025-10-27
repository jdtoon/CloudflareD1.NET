using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

namespace IndexesSample;

[Index(nameof(Email), IsUnique = true, Name = "idx_unique_email")]
[Index(nameof(FirstName), nameof(LastName))]
public class Customer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }
}

public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<Customer> Customers { get; set; } = null!;
    public D1Set<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent index configuration for Product
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique()
            .HasName("idx_unique_sku");

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name);
    }
}
