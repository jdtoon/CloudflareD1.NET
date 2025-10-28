using System;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class SaveChangesTests
{
    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class PeopleContext : D1Context
    {
        public PeopleContext(D1Client client) : base(client) { }

        public D1Set<Person> People { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // defaults
        }
    }

    [Fact]
    public async Task SaveChanges_InsertUpdateDelete_Works()
    {
        // Arrange: use local SQLite mode with a temp file
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_savechanges_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        // Create table
        await client.ExecuteAsync("CREATE TABLE persons (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL)");

        var ctx = new PeopleContext(client);

        // Insert
        var p = new Person { Name = "Alice" };
        ctx.People.Add(p);
        var changes = await ctx.SaveChangesAsync();
        Assert.Equal(1, changes);
        Assert.True(p.Id > 0); // PK populated

        // Verify
        var list = await ctx.People.AsQueryable().ToListAsync();
        Assert.Single(list);
        Assert.Equal("Alice", list.First().Name);

        // Update
        p.Name = "Alice Smith";
        ctx.People.Update(p);
        changes = await ctx.SaveChangesAsync();
        Assert.Equal(1, changes);

        list = await ctx.People.AsQueryable().ToListAsync();
        Assert.Single(list);
        Assert.Equal("Alice Smith", list.First().Name);

        // Delete
        ctx.People.Remove(p);
        changes = await ctx.SaveChangesAsync();
        Assert.Equal(1, changes);

        list = await ctx.People.AsQueryable().ToListAsync();
        Assert.Empty(list);
    }
}
