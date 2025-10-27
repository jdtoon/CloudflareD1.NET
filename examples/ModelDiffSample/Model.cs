using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;

namespace ModelDiffSample;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AppDbContext : D1Context
{
    public AppDbContext(D1Client client) : base(client) {}

    public D1Set<User> Users { get; set; } = null!;
}
