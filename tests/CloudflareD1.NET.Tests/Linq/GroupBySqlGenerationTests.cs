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
    }
}
