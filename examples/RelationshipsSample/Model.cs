using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

namespace RelationshipsSample;

public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // Navigation for Posts
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}

public class Post
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("user_id")]
    public int UserId { get; set; }

    // Navigation to principal
    public User User { get; set; } = null!;
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<User> Users { get; set; } = null!;
    public D1Set<Post> Posts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent relationship configuration to ensure CLI instantiation path invokes this
        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId);

        // Apply additional configuration after the relationship exists
        modelBuilder.Entity<Post>().HasOne(p => p.User).IsRequired();
        modelBuilder.Entity<Post>().HasOne(p => p.User).OnDelete(DeleteBehavior.Cascade);
    }
}
