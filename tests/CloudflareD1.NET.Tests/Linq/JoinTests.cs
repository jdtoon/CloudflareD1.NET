using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using CloudflareD1.NET.Models;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests.Linq
{
    public class JoinTests
    {
        // Test entities
        public class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public decimal Total { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class OrderWithCustomer
        {
            public int OrderId { get; set; }
            public int CustomerId { get; set; }
            public decimal Total { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
        }

        [Fact]
        public async Task Join_InnerJoin_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>
                    {
                        new Dictionary<string, object?>
                        {
                            ["order_id"] = 1,
                            ["customer_id"] = 100,
                            ["total"] = 99.99m,
                            ["customer_name"] = "John Doe",
                            ["customer_email"] = "john@example.com"
                        }
                    }
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var results = await ordersQuery
                .Join(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    Total = order.Total,
                    CustomerName = customer.Name,
                    CustomerEmail = customer.Email
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("SELECT", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FROM orders", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("INNER JOIN customers", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ON orders.customer_id = customers.id", capturedSql, StringComparison.OrdinalIgnoreCase);

            // Verify results
            Assert.Single(results);
            var result = results.First();
            Assert.Equal(1, result.OrderId);
            Assert.Equal("John Doe", result.CustomerName);
        }

        [Fact]
        public async Task LeftJoin_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>
                    {
                        new Dictionary<string, object?>
                        {
                            ["order_id"] = 1,
                            ["customer_id"] = 100,
                            ["total"] = 99.99m,
                            ["customer_name"] = "John Doe",
                            ["customer_email"] = "john@example.com"
                        }
                    }
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var results = await ordersQuery
                .LeftJoin(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    Total = order.Total,
                    CustomerName = customer.Name,
                    CustomerEmail = customer.Email
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("LEFT JOIN customers", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ON orders.customer_id = customers.id", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Join_WithWhere_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>()
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var results = await ordersQuery
                .Join(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    Total = order.Total,
                    CustomerName = customer.Name,
                    CustomerEmail = customer.Email
                })
                .Where(result => result.Total > 50)
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("INNER JOIN", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("WHERE", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Join_WithOrderBy_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>()
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var results = await ordersQuery
                .Join(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    Total = order.Total,
                    CustomerName = customer.Name
                })
                .OrderByDescending("total")
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("INNER JOIN", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("total DESC", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Join_WithTake_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>()
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var results = await ordersQuery
                .Join(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    Total = order.Total,
                    CustomerName = customer.Name
                })
                .Take(10)
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("INNER JOIN", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIMIT 10", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Join_CountAsync_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string? capturedSql = null;

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, object, System.Threading.CancellationToken>((sql, _, __) => capturedSql = sql)
                .ReturnsAsync(new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["count"] = 42 }
                    }
                });

            // Act
            var ordersQuery = new QueryBuilder<Order>(mockClient.Object, "orders");
            var customersQuery = new QueryBuilder<Customer>(mockClient.Object, "customers");

            var count = await ordersQuery
                .Join(
                    customersQuery,
                    order => order.CustomerId,
                    customer => customer.Id)
                .Select((order, customer) => new OrderWithCustomer
                {
                    OrderId = order.Id,
                    Total = order.Total,
                    CustomerName = customer.Name
                })
                .CountAsync();

            // Assert
            Assert.Equal(42, count);
            Assert.NotNull(capturedSql);
            Assert.Contains("SELECT COUNT(*)", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("INNER JOIN", capturedSql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
