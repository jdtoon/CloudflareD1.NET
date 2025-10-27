using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

namespace ModelDiffSample;

public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    // Uncomment this property and rebuild to demonstrate incremental diff
    // [Column("email")]
    // public string? Email { get; set; }
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<User> Users { get; set; } = null!;
}
