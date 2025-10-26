using System;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Mapping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class D1ClientExtensionsTests : IDisposable
    {
        private readonly D1Client _client;
        private readonly Mock<ILogger<D1Client>> _mockLogger;

        public D1ClientExtensionsTests()
        {
            _mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), _mockLogger.Object);

            // Set up test data
            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY,
                    product_name TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock_quantity INTEGER NOT NULL,
                    is_available INTEGER DEFAULT 1
                )");

            await _client.ExecuteAsync("INSERT INTO products (id, product_name, price, stock_quantity, is_available) VALUES (1, 'Widget', 19.99, 100, 1)");
            await _client.ExecuteAsync("INSERT INTO products (id, product_name, price, stock_quantity, is_available) VALUES (2, 'Gadget', 29.99, 50, 1)");
            await _client.ExecuteAsync("INSERT INTO products (id, product_name, price, stock_quantity, is_available) VALUES (3, 'Doohickey', 9.99, 0, 0)");
        }

        public class Product
        {
            public int Id { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public double Price { get; set; }
            public int StockQuantity { get; set; }
            public bool IsAvailable { get; set; }
        }

        [Fact]
        public void Query_ReturnsQueryBuilder()
        {
            // Act
            var query = _client.Query<Product>("products");

            // Assert
            query.Should().NotBeNull();
        }

        [Fact]
        public async Task Query_WithFluentInterface_ExecutesQueryAsync()
        {
            // Act
            var products = await _client.Query<Product>("products")
                .Where("is_available = ?", 1)
                .OrderBy("price")
                .ToListAsync();

            // Assert
            products.Should().HaveCount(2);
            var productList = products.ToList();
            productList[0].ProductName.Should().Be("Widget");
            productList[1].ProductName.Should().Be("Gadget");
        }

        [Fact]
        public async Task QueryAsync_WithSimpleSelect_ReturnsMappedEntitiesAsync()
        {
            // Act
            var products = await _client.QueryAsync<Product>("SELECT * FROM products WHERE is_available = ?", new object[] { 1 });

            // Assert
            products.Should().NotBeNull();
            products.Should().HaveCount(2);
            products.Should().OnlyContain(p => p.IsAvailable);
        }

        [Fact]
        public async Task QueryAsync_MapsSnakeCaseToPascalCaseAsync()
        {
            // Act
            var products = await _client.QueryAsync<Product>("SELECT * FROM products WHERE id = ?", new object[] { 1 });

            // Assert
            var product = products.First();
            product.ProductName.Should().Be("Widget");
            product.Price.Should().BeApproximately(19.99, 0.01);
            product.StockQuantity.Should().Be(100);
            product.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task QueryAsync_WithNoParameters_WorksAsync()
        {
            // Act
            var products = await _client.QueryAsync<Product>("SELECT * FROM products");

            // Assert
            products.Should().HaveCount(3);
        }

        [Fact]
        public async Task QueryAsync_WithMultipleParameters_WorksAsync()
        {
            // Act
            var products = await _client.QueryAsync<Product>(
                "SELECT * FROM products WHERE price >= ? AND stock_quantity > ?",
                new object[] { 15.0, 0 });

            // Assert
            products.Should().HaveCount(2);
            products.Should().OnlyContain(p => p.Price >= 15.0 && p.StockQuantity > 0);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_WithResults_ReturnsFirstAsync()
        {
            // Act
            var product = await _client.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM products ORDER BY price");

            // Assert
            product.Should().NotBeNull();
            product!.ProductName.Should().Be("Doohickey");
            product.Price.Should().BeApproximately(9.99, 0.01);
        }

        [Fact]
        public async Task QueryFirstOrDefaultAsync_WithNoResults_ReturnsNullAsync()
        {
            // Act
            var product = await _client.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM products WHERE price > ?", new object[] { 100.0 });

            // Assert
            product.Should().BeNull();
        }

        [Fact]
        public async Task QuerySingleAsync_WithOneResult_ReturnsSingleAsync()
        {
            // Act
            var product = await _client.QuerySingleAsync<Product>(
                "SELECT * FROM products WHERE id = ?", new object[] { 1 });

            // Assert
            product.Should().NotBeNull();
            product.Id.Should().Be(1);
            product.ProductName.Should().Be("Widget");
        }

        [Fact]
        public async Task QuerySingleAsync_WithNoResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _client.QuerySingleAsync<Product>(
                    "SELECT * FROM products WHERE id = ?", new object[] { 999 }));
        }

        [Fact]
        public async Task QuerySingleAsync_WithMultipleResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _client.QuerySingleAsync<Product>("SELECT * FROM products"));
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_WithOneResult_ReturnsSingleAsync()
        {
            // Act
            var product = await _client.QuerySingleOrDefaultAsync<Product>(
                "SELECT * FROM products WHERE id = ?", new object[] { 1 });

            // Assert
            product.Should().NotBeNull();
            product!.Id.Should().Be(1);
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_WithNoResults_ReturnsNullAsync()
        {
            // Act
            var product = await _client.QuerySingleOrDefaultAsync<Product>(
                "SELECT * FROM products WHERE id = ?", new object[] { 999 });

            // Assert
            product.Should().BeNull();
        }

        [Fact]
        public async Task QuerySingleOrDefaultAsync_WithMultipleResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _client.QuerySingleOrDefaultAsync<Product>("SELECT * FROM products"));
        }

        [Fact]
        public async Task Query_WithCustomMapper_UsesCustomMapperAsync()
        {
            // Arrange
            var customMapper = new Mock<IEntityMapper>();
            var expectedProducts = new[] { new Product { Id = 1, ProductName = "Custom", Price = 99.99, StockQuantity = 10, IsAvailable = true } };
            customMapper.Setup(m => m.MapMany<Product>(It.IsAny<System.Collections.Generic.IEnumerable<System.Collections.Generic.Dictionary<string, object?>>>()))
                .Returns(expectedProducts);

            // Act
            var products = await _client.QueryAsync<Product>(
                "SELECT * FROM products",
                mapper: customMapper.Object);

            // Assert
            products.Should().BeEquivalentTo(expectedProducts);
            customMapper.Verify(m => m.MapMany<Product>(It.IsAny<System.Collections.Generic.IEnumerable<System.Collections.Generic.Dictionary<string, object?>>>()), Times.Once);
        }

        [Fact]
        public async Task Query_WithTableName_CanExecuteFluentQueryAsync()
        {
            // Act
            var count = await _client.Query<Product>("products")
                .Where("is_available = ?", 1)
                .CountAsync();

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public async Task Query_WithPagination_WorksCorrectlyAsync()
        {
            // Act
            var page1 = await _client.Query<Product>("products")
                .OrderBy("id")
                .Take(2)
                .ToListAsync();

            var page2 = await _client.Query<Product>("products")
                .OrderBy("id")
                .Skip(2)
                .Take(2)
                .ToListAsync();

            // Assert
            page1.Should().HaveCount(2);
            page1.First().Id.Should().Be(1);

            page2.Should().HaveCount(1);
            page2.First().Id.Should().Be(3);
        }

        [Fact]
        public async Task QueryAsync_WithComplexTypes_MapsCorrectlyAsync()
        {
            // Arrange
            await _client.ExecuteAsync(@"
                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY,
                    order_date TEXT NOT NULL,
                    total_amount REAL NOT NULL
                )");

            await _client.ExecuteAsync(
                "INSERT INTO orders (id, order_date, total_amount) VALUES (?, ?, ?)",
                new object[] { 1, "2024-01-15T10:30:00Z", 149.99 });

            // Act
            var orders = await _client.QueryAsync<Order>("SELECT * FROM orders");

            // Assert
            var order = orders.First();
            order.Id.Should().Be(1);
            order.OrderDate.Should().BeCloseTo(DateTime.Parse("2024-01-15T10:30:00Z"), TimeSpan.FromSeconds(1));
            order.TotalAmount.Should().BeApproximately(149.99, 0.01);
        }

        public class Order
        {
            public int Id { get; set; }
            public DateTime OrderDate { get; set; }
            public double TotalAmount { get; set; }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
