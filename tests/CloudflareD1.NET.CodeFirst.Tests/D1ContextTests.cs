using System;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class D1ContextTests
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
            // No explicit configuration needed for this test
        }
    }

    [Fact]
    public void Context_Initializes_Sets_And_Model()
    {
        // Arrange: use local SQLite mode with a temp file
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_test_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        // Act
        var ctx = new PeopleContext(client);

        // Assert: D1Set property initialized
        Assert.NotNull(ctx.People);
    Assert.Equal("persons", ctx.People.TableName);

        // Assert: Model built
        var entity = ctx.Model.GetEntity<Person>();
        Assert.NotNull(entity);
    Assert.Equal("persons", entity!.TableName);
        Assert.Single(entity.PrimaryKey);
        Assert.Equal("id", entity.PrimaryKey[0].ColumnName);
    }
}
