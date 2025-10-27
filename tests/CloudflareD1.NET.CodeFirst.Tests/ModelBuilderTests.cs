using System;
using System.Linq;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class ModelBuilderTests
{
    [Table("widgets")]
    private class Widget
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private class Gadget
    {
        public int GadgetId { get; set; }
        public string? Description { get; set; }
    }

    private class TestContext : D1Context
    {
        public TestContext(D1Client client) : base(client) { }
        public D1Set<Widget> Widgets { get; set; } = null!;
        public D1Set<Gadget> Gadgets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Explicitly register entities (optional, as discovery will pick up D1Set properties)
            modelBuilder.Entity<Widget>();
            modelBuilder.Entity<Gadget>();
        }
    }

    [Fact]
    public void Build_Model_Uses_Attributes_And_Conventions()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_test_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = tempDb });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        var ctx = new TestContext(client);

        var model = ctx.Model;

        var widget = model.GetEntity(typeof(Widget));
        Assert.NotNull(widget);
        Assert.Equal("widgets", widget!.TableName);
        Assert.Equal(3, widget.Properties.Count);
        Assert.Single(widget.PrimaryKey);
        Assert.Equal("id", widget.PrimaryKey[0].ColumnName);
        Assert.Equal("INTEGER", widget.PrimaryKey[0].ColumnType);

        var gadget = model.GetEntity(typeof(Gadget));
        Assert.NotNull(gadget);
        Assert.Equal("gadgets", gadget!.TableName);
        Assert.Equal(2, gadget.Properties.Count);
        Assert.Single(gadget.PrimaryKey);
        Assert.Equal("gadget_id", gadget.PrimaryKey[0].ColumnName);
        Assert.Equal("INTEGER", gadget.PrimaryKey[0].ColumnType);
    }
}
