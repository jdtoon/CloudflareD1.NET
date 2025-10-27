using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

namespace TestCloudflareD1.Models;

[Index(nameof(Email), IsUnique = true)]
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

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}

[Index(nameof(CustomerId))]
[Index(nameof(Status))]
public class Order
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Required]
    [Column("total")]
    public decimal Total { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("order_date")]
    public DateTime? OrderDate { get; set; }

    // Navigation property
    public Customer Customer { get; set; } = null!;
}

public class TestAppDbContext : D1Context
{
    public TestAppDbContext(D1Client client) : base(client) {}

    public D1Set<Customer> Customers { get; set; } = null!;
    public D1Set<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId);

        modelBuilder.Entity<Order>().HasOne(o => o.Customer).OnDelete(DeleteBehavior.Cascade);
    }
}
