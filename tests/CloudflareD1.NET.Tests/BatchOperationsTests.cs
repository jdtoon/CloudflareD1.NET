using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    /// <summary>
    /// Tests for batch operation extension methods.
    /// </summary>
    public class BatchOperationsTests : IDisposable
    {
        private readonly D1Client _client;

        public BatchOperationsTests()
        {
            var mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), mockLogger.Object);

            // Setup test table
            SetupTestTableAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestTableAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL
                );
            ");
        }

        [Fact]
        public async Task BatchInsertAsync_InsertsMultipleEntities()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Name = "Product A", Price = 10.99, Stock = 100 },
                new Product { Name = "Product B", Price = 20.99, Stock = 50 },
                new Product { Name = "Product C", Price = 15.99, Stock = 75 }
            };

            // Act
            var results = await _client.BatchInsertAsync("products", products);

            // Assert
            Assert.Equal(3, results.Length);
            Assert.All(results, r => Assert.True(r.Success));

            var queryResult = await _client.QueryAsync("SELECT * FROM products");
            Assert.Equal(3, queryResult.Results!.Count);
        }

        [Fact]
        public async Task BatchInsertAsync_WithEmptyList_ReturnsEmptyArray()
        {
            // Arrange
            var products = new List<Product>();

            // Act
            var results = await _client.BatchInsertAsync("products", products);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task BatchUpdateAsync_UpdatesMultipleEntities()
        {
            // Arrange - Insert test data
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (1, 'Product A', 10.99, 100)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (2, 'Product B', 20.99, 50)");

            var updatedProducts = new List<Product>
            {
                new Product { Id = 1, Name = "Updated A", Price = 12.99, Stock = 90 },
                new Product { Id = 2, Name = "Updated B", Price = 22.99, Stock = 45 }
            };

            // Act
            var results = await _client.BatchUpdateAsync("products", updatedProducts, p => p.Id);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, r => Assert.True(r.Success));

            var queryResult = await _client.QueryAsync("SELECT * FROM products ORDER BY id");
            Assert.Equal("Updated A", queryResult.Results![0]["name"]);
            Assert.Equal(12.99, Convert.ToDouble(queryResult.Results[0]["price"]));
            Assert.Equal("Updated B", queryResult.Results[1]["name"]);
            Assert.Equal(22.99, Convert.ToDouble(queryResult.Results[1]["price"]));
        }

        [Fact]
        public async Task BatchDeleteAsync_DeletesMultipleEntities()
        {
            // Arrange - Insert test data
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (1, 'Product A', 10.99, 100)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (2, 'Product B', 20.99, 50)");
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (3, 'Product C', 15.99, 75)");

            var idsToDelete = new List<int> { 1, 3 };

            // Act
            var results = await _client.BatchDeleteAsync("products", idsToDelete);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, r => Assert.True(r.Success));

            var queryResult = await _client.QueryAsync("SELECT * FROM products");
            Assert.Single(queryResult.Results!);
            Assert.Equal("Product B", queryResult.Results[0]["name"]);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithCustomKeyColumn_DeletesCorrectly()
        {
            // Arrange - Create table with custom key
            await _client.ExecuteAsync(@"
                CREATE TABLE users (
                    user_id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL
                );
            ");
            await _client.ExecuteAsync("INSERT INTO users (user_id, name) VALUES (101, 'Alice')");
            await _client.ExecuteAsync("INSERT INTO users (user_id, name) VALUES (102, 'Bob')");

            // Act
            var results = await _client.BatchDeleteAsync("users", new[] { 101 }, keyColumnName: "user_id");

            // Assert
            Assert.Single(results);
            var queryResult = await _client.QueryAsync("SELECT * FROM users");
            Assert.Single(queryResult.Results!);
            Assert.Equal("Bob", queryResult.Results[0]["name"]);
        }

        [Fact]
        public async Task UpsertAsync_InsertsNewEntity()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "New Product", Price = 25.99, Stock = 30 };

            // Act
            var result = await _client.UpsertAsync("products", product);

            // Assert
            Assert.True(result.Success);
            var queryResult = await _client.QueryAsync("SELECT * FROM products WHERE id = 1");
            Assert.Single(queryResult.Results!);
            Assert.Equal("New Product", queryResult.Results[0]["name"]);
        }

        [Fact]
        public async Task UpsertAsync_UpdatesExistingEntity()
        {
            // Arrange - Insert existing
            await _client.ExecuteAsync("INSERT INTO products (id, name, price, stock) VALUES (1, 'Original', 10.99, 100)");

            var product = new Product { Id = 1, Name = "Updated", Price = 12.99, Stock = 90 };

            // Act
            var result = await _client.UpsertAsync("products", product);

            // Assert
            Assert.True(result.Success);
            var queryResult = await _client.QueryAsync("SELECT * FROM products WHERE id = 1");
            Assert.Single(queryResult.Results!);
            Assert.Equal("Updated", queryResult.Results[0]["name"]);
            Assert.Equal(12.99, Convert.ToDouble(queryResult.Results[0]["price"]));
        }

        [Fact]
        public async Task BatchInsertAsync_HandlesLargeNumberOfEntities()
        {
            // Arrange
            var products = Enumerable.Range(1, 50).Select(i => new Product
            {
                Name = $"Product {i}",
                Price = 10.0 + i,
                Stock = i * 10
            }).ToList();

            // Act
            var results = await _client.BatchInsertAsync("products", products);

            // Assert
            Assert.Equal(50, results.Length);
            Assert.All(results, r => Assert.True(r.Success));

            var queryResult = await _client.QueryAsync("SELECT COUNT(*) as count FROM products");
            Assert.Equal(50L, queryResult.Results![0]["count"]);
        }

        [Fact]
        public async Task BatchInsertAsync_WithNullParameter_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _client.BatchInsertAsync<Product>("products", null!));
        }

        [Fact]
        public async Task BatchUpdateAsync_WithNullKeySelector_ThrowsException()
        {
            // Arrange
            var products = new List<Product> { new Product { Id = 1, Name = "Test", Price = 10, Stock = 5 } };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _client.BatchUpdateAsync("products", products, null!));
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public double Price { get; set; }
            public int Stock { get; set; }
        }
    }
}
