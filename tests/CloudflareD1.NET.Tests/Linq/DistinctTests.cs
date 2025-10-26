using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CloudflareD1.NET.Tests.Linq
{
    public class DistinctTests : IDisposable
    {
        private readonly D1Client _client;

        public DistinctTests()
        {
            var mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), mockLogger.Object);

            // Setup test data
            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    price REAL NOT NULL
                )");

            await _client.ExecuteAsync("INSERT INTO products (id, name, category, price) VALUES (1, 'Product A', 'Electronics', 99.99)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, category, price) VALUES (2, 'Product B', 'Electronics', 149.99)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, category, price) VALUES (3, 'Product C', 'Books', 19.99)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, category, price) VALUES (4, 'Product D', 'Electronics', 99.99)"); // Duplicate price
            await _client.ExecuteAsync("INSERT INTO products (id, name, category, price) VALUES (5, 'Product E', 'Books', 29.99)");
        }

        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public decimal Price { get; set; }
        }

        [Fact]
        public async Task Distinct_ReturnsUniqueRows()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Distinct()
                .ToListAsync();

            // Assert
            Assert.Equal(5, result.Count()); // All rows are unique
        }

        [Fact]
        public async Task Distinct_WithWhere_FiltersAndReturnsUnique()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Where("category = ?", "Electronics")
                .Distinct()
                .ToListAsync();

            // Assert
            Assert.Equal(3, result.Count()); // 3 electronics products
        }

        [Fact]
        public async Task Distinct_WithOrderBy_OrdersResults()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Distinct()
                .OrderBy("category")
                .ToListAsync();

            // Assert
            Assert.Equal(5, result.Count());
            var categories = result.Select(p => p.Category).ToList();
            Assert.Equal("Books", categories[0]);
            Assert.Equal("Books", categories[1]);
        }

        [Fact]
        public async Task Distinct_WithTake_LimitsResults()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Distinct()
                .Take(3)
                .ToListAsync();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task Distinct_WithSelect_ReturnsUniqueProjections()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Select(p => new Product { Category = p.Category })
                .Distinct()
                .ToListAsync();

            // Assert - Should have 2 unique categories
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Distinct_ComplexQuery_CombinesFeatures()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Where("price > ?", 20.0m)
                .Distinct()
                .OrderBy("price")
                .Take(3)
                .ToListAsync();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.True(result.All(p => p.Price > 20m));
        }

        [Fact]
        public async Task Distinct_EmptyResults_ReturnsEmptyList()
        {
            // Arrange & Act
            var result = await _client.Query<Product>("products")
                .Where("category = ?", "NonExistent")
                .Distinct()
                .ToListAsync();

            // Assert
            Assert.Empty(result);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}

