using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests.Linq
{
    public class GroupByTests
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
            public int Count { get; set; }
            public decimal TotalPrice { get; set; }
        }

        [Fact]
        public void GroupBy_SingleKey_CreatesGroupByQueryBuilder()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            Expression<Func<Product, string>> keySelector = p => p.Category;

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var groupByBuilder = queryBuilder.GroupBy(keySelector);

            // Assert
            Assert.NotNull(groupByBuilder);
            Assert.IsAssignableFrom<IGroupByQueryBuilder<Product, string>>(groupByBuilder);
        }

        [Fact]
        public void GroupBy_WithSelect_CreatesProjectionBuilder()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();

            // Act
            var queryBuilder = new QueryBuilder<Product>(mockClient.Object, "products");
            var groupedQuery = queryBuilder
                .GroupBy(p => p.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    Count = 0  // Will be replaced with aggregate in real implementation
                });

            // Assert
            Assert.NotNull(groupedQuery);
            Assert.IsAssignableFrom<IGroupByProjectionQueryBuilder<CategorySummary>>(groupedQuery);
        }
    }
}
