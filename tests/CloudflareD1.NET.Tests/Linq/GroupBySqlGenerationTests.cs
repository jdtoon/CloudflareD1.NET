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
    public class GroupBySqlGenerationTests
    {
        public class Product
        {
            public int Id { get; set; }
            public string Category { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        public class CategorySummary
        {
            public string Category { get; set; } = string.Empty;
            public int ProductCount { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal AveragePrice { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public int Count { get; set; }
            public decimal Total { get; set; }
            public decimal TotalValue { get; set; }
        }

        [Fact]
        public async Task GroupBy_WithCount_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
                            ["category"] = "Electronics",
                            ["product_count"] = 5
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    ProductCount = g.Count()
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("SELECT", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("category", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("COUNT(*)", capturedSql, StringComparison.OrdinalIgnoreCase);

            // Verify results
            Assert.Single(results);
            var result = results.First();
            Assert.Equal("Electronics", result.Category);
            Assert.Equal(5, result.ProductCount);
        }

        [Fact]
        public async Task GroupBy_WithSum_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
                            ["category"] = "Electronics",
                            ["total_price"] = 999.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    TotalPrice = g.Sum(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("SELECT", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SUM", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price", capturedSql, StringComparison.OrdinalIgnoreCase);

            // Verify results
            Assert.Single(results);
            var result = results.First();
            Assert.Equal("Electronics", result.Category);
            Assert.Equal(999.99m, result.TotalPrice);
        }

        [Fact]
        public async Task GroupBy_WithMultipleAggregates_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
                            ["category"] = "Electronics",
                            ["product_count"] = 5,
                            ["total_price"] = 999.99m,
                            ["average_price"] = 199.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    ProductCount = g.Count(),
                    TotalPrice = g.Sum(p => p.Price),
                    AveragePrice = g.Average(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("COUNT(*)", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SUM", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AVG", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);

            // Verify results
            Assert.Single(results);
            var result = results.First();
            Assert.Equal("Electronics", result.Category);
            Assert.Equal(5, result.ProductCount);
            Assert.Equal(999.99m, result.TotalPrice);
            Assert.Equal(199.99m, result.AveragePrice);
        }

        [Fact]
        public async Task GroupBy_WithMin_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
                            ["category"] = "Electronics",
                            ["min_price"] = 49.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    MinPrice = g.Min(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("MIN", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GroupBy_WithMax_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
                            ["category"] = "Electronics",
                            ["max_price"] = 999.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    MaxPrice = g.Max(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("MAX", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GroupBy_WithWhere_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .Where(p => p.Price > 50)
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("WHERE", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price > ?", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GroupBy_WithOrderBy_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    Total = g.Sum(p => p.Price)
                })
                .OrderBy("total")
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("total", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GroupBy_WithTake_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .Take(5)
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIMIT 5", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GroupBy_WithComplexAggregateExpression_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            string capturedSql = null!;

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
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    TotalValue = g.Sum(p => p.Price * p.Quantity)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("SUM", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("price", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("quantity", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("*", capturedSql); // multiplication
        }
    }
}

