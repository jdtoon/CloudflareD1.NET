using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CodeFirst.Sample.Models;

namespace CodeFirst.Sample;

/// <summary>
/// Application database context
/// </summary>
public class BlogDbContext : D1Context
{
    public BlogDbContext(D1Client client) : base(client) { }

    public D1Set<User> Users { get; set; } = null!;
    public D1Set<BlogPost> BlogPosts { get; set; } = null!;
    public D1Set<Comment> Comments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User entity
        modelBuilder.Entity<User>()
            .ToTable("users")
            .HasKey(u => u.Id);

        // Configure BlogPost entity
        modelBuilder.Entity<BlogPost>()
            .ToTable("blog_posts")
            .HasKey(b => b.Id);

        // Configure Comment entity
        modelBuilder.Entity<Comment>()
            .ToTable("comments")
            .HasKey(c => c.Id);
    }
}
