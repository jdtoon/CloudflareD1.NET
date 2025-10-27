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

public class CompositeKeyTests
{
    private class Order
    {
        [Key]
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
    }

    private class OrderItem
    {
        public int OrderId { get; set; }
        public int LineNumber { get; set; }
        public string Sku { get; set; } = string.Empty;

        [ForeignKey(nameof(Order))]
        public Order? Order { get; set; }
    }

    private class ShopContext : D1Context
    {
        public ShopContext(D1Client client) : base(client) {}
        public D1Set<Order> Orders { get; set; } = null!;
        public D1Set<OrderItem> OrderItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var b = modelBuilder.Entity<OrderItem>();
            b.HasKey(oi => new { oi.OrderId, oi.LineNumber });
            b.HasOne<Order>(oi => oi.Order)
             .WithMany()
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    [Fact]
    public void Composite_Key_Generates_Table_Level_PK()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cktest_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = tempDb });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        var ctx = new ShopContext(client);

        var model = ctx.Model;
        var items = model.GetEntity(typeof(OrderItem));
        Assert.NotNull(items);
        Assert.Equal(2, items!.PrimaryKey.Count);
        Assert.Contains(items.PrimaryKey, p => p.PropertyInfo.Name == nameof(OrderItem.OrderId));
        Assert.Contains(items.PrimaryKey, p => p.PropertyInfo.Name == nameof(OrderItem.LineNumber));

        var schema = ModelSchemaConverter.ToDatabaseSchema(model);
        var itemsTable = schema.Tables.First(t => t.Name == "order_items");

        // Scaffold and ensure table-level PK call exists
        var scaffolder = new MigrationScaffolder();
        var code = scaffolder.GenerateMigration(null, schema, "Init");
        Assert.Contains("t.PrimaryKey(\"order_id\", \"line_number\")", code);
    }
}
