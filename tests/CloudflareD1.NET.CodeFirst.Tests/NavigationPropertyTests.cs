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

public class NavigationPropertyTests
{
    private class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public double Total { get; set; }
        public DateTime? OrderDate { get; set; }

        // Navigation - should be ignored by model discovery
        public Customer? Customer { get; set; }
    }

    private class TestContext : D1Context
    {
        public TestContext(D1Client client) : base(client) { }
        public D1Set<Customer> Customers { get; set; } = null!;
        public D1Set<Order> Orders { get; set; } = null!;
    }

    [Fact]
    public void ModelBuilder_Ignores_Navigation_Properties()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_nav_test_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = tempDb });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        var ctx = new TestContext(client);

        var model = ctx.Model;
        var orders = model.GetEntity(typeof(Order));
        Assert.NotNull(orders);

        // Ensure the 'customer' navigation is not mapped as a column
        var columnNames = orders!.Properties.Select(p => p.ColumnName).ToArray();
        Assert.DoesNotContain("customer", columnNames);

        // Ensure expected scalar properties are present
        Assert.Contains(columnNames, c => c == "id");
        Assert.Contains(columnNames, c => c == "customer_id");
        Assert.Contains(columnNames, c => c == "total");
        Assert.Contains(columnNames, c => c == "order_date");
    }
}
