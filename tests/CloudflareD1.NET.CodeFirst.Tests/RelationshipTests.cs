using System;
using System.Linq;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.Converters;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class RelationshipTests
{
    private class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Post
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey(nameof(User))]
        public User? User { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private class BlogContext : D1Context
    {
        public BlogContext(D1Client client) : base(client) { }
        public D1Set<User> Users { get; set; } = null!;
        public D1Set<Post> Posts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Fluent relationship (will supplement the attribute)
            modelBuilder
                .Entity<Post>()
                .HasOne<User>(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    [Fact]
    public void Model_Builds_FK_From_Fluent_And_Attribute()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"reltest_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = tempDb });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        var ctx = new BlogContext(client);

        var model = ctx.Model;
        var posts = model.GetEntity(typeof(Post));
        var users = model.GetEntity(typeof(User));
        Assert.NotNull(posts);
        Assert.NotNull(users);

        // FK metadata present
        Assert.Single(posts!.ForeignKeys);
        var fk = posts.ForeignKeys[0];
        Assert.Equal(typeof(User), fk.PrincipalType);
        Assert.Equal(typeof(Post), fk.DependentType);
        Assert.Single(fk.DependentProperties);
        Assert.Equal("user_id", fk.DependentProperties[0].ColumnName);
        Assert.Equal("CASCADE", fk.OnDelete);

        // Converter -> DatabaseSchema
        var schema = ModelSchemaConverter.ToDatabaseSchema(model);
        var postsTable = schema.Tables.First(t => t.Name == "posts");
        Assert.Single(postsTable.ForeignKeys);
        var fkSchema = postsTable.ForeignKeys[0];
        Assert.Equal("user_id", fkSchema.Column);
        Assert.Equal("users", fkSchema.ReferencedTable);
        Assert.Equal("id", fkSchema.ReferencedColumn);
        Assert.Equal("CASCADE", fkSchema.OnDelete);

        // Scaffolder emits ForeignKey call
        var scaffolder = new MigrationScaffolder();
        var code = scaffolder.GenerateMigration(null, schema, "Init");
        Assert.Contains("t.ForeignKey(\"user_id\", \"users\", \"id\", \"CASCADE\")", code);
    }
}
