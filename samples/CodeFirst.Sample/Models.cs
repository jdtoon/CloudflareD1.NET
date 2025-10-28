using System.ComponentModel.DataAnnotations;
using CloudflareD1.NET.CodeFirst.Attributes;
using KeyAttribute = CloudflareD1.NET.CodeFirst.Attributes.KeyAttribute;
using RequiredAttribute = CloudflareD1.NET.CodeFirst.Attributes.RequiredAttribute;

namespace CodeFirst.Sample.Models;

/// <summary>
/// User entity representing application users
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(200)]
    public string? FullName { get; set; }

    [Column("bio")]
    [StringLength(500)]
    public string? Bio { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<BlogPost> BlogPosts { get; set; } = new();
}

/// <summary>
/// Blog post entity
/// </summary>
[Table("blog_posts")]
public class BlogPost
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("title")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("author_id")]
    [ForeignKey(nameof(Author))]
    public int AuthorId { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User Author { get; set; } = null!;

    // Navigation property
    public List<Comment> Comments { get; set; } = new();
}

/// <summary>
/// Comment entity for blog posts
/// </summary>
[Table("comments")]
public class Comment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("post_id")]
    [ForeignKey(nameof(BlogPost))]
    public int PostId { get; set; }

    [Column("author_id")]
    [ForeignKey(nameof(Author))]
    public int AuthorId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public BlogPost BlogPost { get; set; } = null!;
    public User Author { get; set; } = null!;
}
