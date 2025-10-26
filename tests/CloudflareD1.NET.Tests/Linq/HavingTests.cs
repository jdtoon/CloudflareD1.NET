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
    public class HavingTests
    {
        // Test entity
        public class Product
        {
            public int Id { get; set; }
            public string Category { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public async Task Having_WithCountGreaterThan_GeneratesCorrectSQL()
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
                            ["category"] = "Electronics",
                            ["count"] = 10
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Having(g => g.Count() > 5)
                .Select(g => new CategoryCount
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("COUNT(*) > 5", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Single(results);
            Assert.Equal(10, results.First().Count);
        }

        [Fact]
        public async Task Having_WithSumGreaterThan_GeneratesCorrectSQL()
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
                            ["category"] = "Electronics",
                            ["total_price"] = 5000.00m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Having(g => g.Sum(p => p.Price) > 1000)
                .Select(g => new CategoryTotal
                {
                    Category = g.Key,
                    TotalPrice = g.Sum(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SUM(price) > 1000", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Having_WithAverageGreaterThanOrEqual_GeneratesCorrectSQL()
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
                            ["category"] = "Electronics",
                            ["average_price"] = 75.50m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Having(g => g.Average(p => p.Price) >= 50)
                .Select(g => new CategoryAverage
                {
                    Category = g.Key,
                    AveragePrice = g.Average(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AVG(price) >= 50", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Having_WithMinLessThan_GeneratesCorrectSQL()
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
                            ["category"] = "Books",
                            ["min_price"] = 5.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Having(g => g.Min(p => p.Price) < 10)
                .Select(g => new CategoryMinPrice
                {
                    Category = g.Key,
                    MinPrice = g.Min(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MIN(price) < 10", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Having_WithMaxLessThanOrEqual_GeneratesCorrectSQL()
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
                            ["category"] = "Accessories",
                            ["max_price"] = 299.99m
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .GroupBy(p => p.Category)
                .Having(g => g.Max(p => p.Price) <= 500)
                .Select(g => new CategoryMaxPrice
                {
                    Category = g.Key,
                    MaxPrice = g.Max(p => p.Price)
                })
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MAX(price) <= 500", capturedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Having_WithWhereAndOrderBy_GeneratesCorrectSQL()
        {
            // Arrange: WHERE filters before grouping, HAVING filters after grouping
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
                            ["category"] = "Electronics",
                            ["count"] = 15
                        }
                    }
                });

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var results = await queryBuilder
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category)
                .Having(g => g.Count() > 3)
                .Select(g => new CategoryCount
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending("count")
                .ToListAsync();

            // Assert
            Assert.NotNull(capturedSql);
            Assert.Contains("WHERE", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("HAVING", capturedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", capturedSql, StringComparison.OrdinalIgnoreCase);

            // Verify correct order: WHERE ... GROUP BY ... HAVING ... ORDER BY
            var whereIndex = capturedSql!.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            var groupByIndex = capturedSql.IndexOf("GROUP BY", StringComparison.OrdinalIgnoreCase);
            var havingIndex = capturedSql.IndexOf("HAVING", StringComparison.OrdinalIgnoreCase);
            var orderByIndex = capturedSql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);

            Assert.True(whereIndex < groupByIndex);
            Assert.True(groupByIndex < havingIndex);
            Assert.True(havingIndex < orderByIndex);
        }

        // Result classes for tests
        public class CategoryCount
        {
            public string Category { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class CategoryTotal
        {
            public string Category { get; set; } = string.Empty;
            public decimal TotalPrice { get; set; }
        }

        public class CategoryAverage
        {
            public string Category { get; set; } = string.Empty;
            public decimal AveragePrice { get; set; }
        }

        public class CategoryMinPrice
        {
            public string Category { get; set; } = string.Empty;
            public decimal MinPrice { get; set; }
        }

        public class CategoryMaxPrice
        {
            public string Category { get; set; } = string.Empty;
            public decimal MaxPrice { get; set; }
        }
    }
}
