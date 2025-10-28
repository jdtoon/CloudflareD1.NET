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

    // Test entity with multiple properties for property change detection tests
    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private class ProductContext : D1Context
    {
        public ProductContext(D1Client client) : base(client) { }

        public D1Set<Product> Products { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // defaults
        }
    }

    [Fact]
    public async Task SaveChanges_UpdateSingleProperty_OnlyUpdatesChangedColumn()
    {
        // Arrange
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_propchange_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        await client.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                updated_at TEXT
            )
        ");

        var ctx = new ProductContext(client);

        // Insert product
        var product = new Product
        {
            Name = "Widget",
            Price = 19.99m,
            Stock = 100,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        Assert.True(product.Id > 0);

        // Update only the Name property
        product.Name = "Super Widget";
        ctx.Products.Update(product);
        var changes = await ctx.SaveChangesAsync();

        // Should affect 1 row
        Assert.Equal(1, changes);

        // Verify the change persisted
        var retrieved = await ctx.Products.AsQueryable().FirstOrDefaultAsync();
        Assert.NotNull(retrieved);
        Assert.Equal("Super Widget", retrieved.Name);
        Assert.Equal(19.99m, retrieved.Price); // unchanged
        Assert.Equal(100, retrieved.Stock); // unchanged
    }

    [Fact]
    public async Task SaveChanges_UpdateMultipleProperties_OnlyUpdatesChangedColumns()
    {
        // Arrange
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_multiprop_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        await client.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                updated_at TEXT
            )
        ");

        var ctx = new ProductContext(client);

        // Insert
        var product = new Product
        {
            Name = "Gadget",
            Price = 29.99m,
            Stock = 50,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        // Update Price and Stock only
        product.Price = 24.99m;
        product.Stock = 45;
        ctx.Products.Update(product);
        var changes = await ctx.SaveChangesAsync();

        Assert.Equal(1, changes);

        // Verify
        var retrieved = await ctx.Products.AsQueryable().FirstOrDefaultAsync();
        Assert.NotNull(retrieved);
        Assert.Equal("Gadget", retrieved.Name); // unchanged
        Assert.Equal(24.99m, retrieved.Price); // changed
        Assert.Equal(45, retrieved.Stock); // changed
        Assert.True(retrieved.IsActive); // unchanged
    }

    [Fact]
    public async Task SaveChanges_UpdateNoProperties_SkipsUpdateStatement()
    {
        // Arrange
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_nochange_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        await client.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                updated_at TEXT
            )
        ");

        var ctx = new ProductContext(client);

        // Insert
        var product = new Product
        {
            Name = "Doohickey",
            Price = 9.99m,
            Stock = 200,
            IsActive = false,
            UpdatedAt = null
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        // Update but don't actually change any values
        ctx.Products.Update(product);
        var changes = await ctx.SaveChangesAsync();

        // Should return 0 because no UPDATE statement was generated
        Assert.Equal(0, changes);

        // Verify data is unchanged
        var retrieved = await ctx.Products.AsQueryable().FirstOrDefaultAsync();
        Assert.NotNull(retrieved);
        Assert.Equal("Doohickey", retrieved.Name);
        Assert.Equal(9.99m, retrieved.Price);
    }

    [Fact]
    public async Task SaveChanges_UpdateAllProperties_UpdatesAllColumns()
    {
        // Arrange
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_allprop_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        await client.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                updated_at TEXT
            )
        ");

        var ctx = new ProductContext(client);

        // Insert
        var product = new Product
        {
            Name = "Thing",
            Price = 14.99m,
            Stock = 10,
            IsActive = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        // Update all properties
        var newDate = DateTime.UtcNow;
        product.Name = "New Thing";
        product.Price = 19.99m;
        product.Stock = 20;
        product.IsActive = true;
        product.UpdatedAt = newDate;
        ctx.Products.Update(product);
        var changes = await ctx.SaveChangesAsync();

        Assert.Equal(1, changes);

        // Verify all changed
        var retrieved = await ctx.Products.AsQueryable().FirstOrDefaultAsync();
        Assert.NotNull(retrieved);
        Assert.Equal("New Thing", retrieved.Name);
        Assert.Equal(19.99m, retrieved.Price);
        Assert.Equal(20, retrieved.Stock);
        Assert.True(retrieved.IsActive);
        Assert.NotNull(retrieved.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_UpdateNullableProperty_DetectsChange()
    {
        // Arrange
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codefirst_nullable_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options
        {
            UseLocalMode = true,
            LocalDatabasePath = tempDb
        });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);

        await client.ExecuteAsync(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                stock INTEGER NOT NULL,
                is_active INTEGER NOT NULL,
                updated_at TEXT
            )
        ");

        var ctx = new ProductContext(client);

        // Insert with null UpdatedAt
        var product = new Product
        {
            Name = "Item",
            Price = 5.99m,
            Stock = 30,
            IsActive = true,
            UpdatedAt = null
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        // Update UpdatedAt from null to a value
        product.UpdatedAt = DateTime.UtcNow;
        ctx.Products.Update(product);
        var changes = await ctx.SaveChangesAsync();

        Assert.Equal(1, changes);

        // Verify
        var retrieved = await ctx.Products.AsQueryable().FirstOrDefaultAsync();
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.UpdatedAt);
    }
}
