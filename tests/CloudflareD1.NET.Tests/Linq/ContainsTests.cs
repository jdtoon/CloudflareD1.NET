using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Linq.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace CloudflareD1.NET.Tests.Linq
{
    public class ContainsTests
    {
        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public decimal Price { get; set; }
        }

        [Fact]
        public void Contains_GeneratesInClause()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var visitor = new SqlExpressionVisitor(mapper);
            var categories = new[] { "Electronics", "Books" };
            Expression<Func<Product, bool>> predicate = p => categories.Contains(p.Category);

            // Act
            var sql = visitor.Translate(predicate.Body);
            var parameters = visitor.GetParameters();

            // Assert
            Assert.Contains(" IN (", sql);
            Assert.Equal(2, parameters.Length);
            Assert.Equal("Electronics", parameters[0]);
            Assert.Equal("Books", parameters[1]);
        }

        [Fact]
        public void Contains_WithEmptyCollection_GeneratesInClause()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var visitor = new SqlExpressionVisitor(mapper);
            var categories = System.Array.Empty<string>();
            Expression<Func<Product, bool>> predicate = p => categories.Contains(p.Category);

            // Act
            var sql = visitor.Translate(predicate.Body);
            var parameters = visitor.GetParameters();

            // Assert
            Assert.Contains(" IN ()", sql);
            Assert.Empty(parameters);
        }

        [Fact]
        public void Contains_WithSingleItem_GeneratesInClause()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var visitor = new SqlExpressionVisitor(mapper);
            var categories = new[] { "Electronics" };
            Expression<Func<Product, bool>> predicate = p => categories.Contains(p.Category);

            // Act
            var sql = visitor.Translate(predicate.Body);
            var parameters = visitor.GetParameters();

            // Assert
            Assert.Contains(" IN (", sql);
            Assert.Single(parameters);
            Assert.Equal("Electronics", parameters[0]);
        }

        [Fact]
        public void Contains_WithIntArray_GeneratesInClause()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var visitor = new SqlExpressionVisitor(mapper);
            var ids = new[] { 1, 3, 5 };
            Expression<Func<Product, bool>> predicate = p => ids.Contains(p.Id);

            // Act
            var sql = visitor.Translate(predicate.Body);
            var parameters = visitor.GetParameters();

            // Assert
            Assert.Contains(" IN (", sql);
            Assert.Equal(3, parameters.Length);
            Assert.Equal(1, parameters[0]);
            Assert.Equal(3, parameters[1]);
            Assert.Equal(5, parameters[2]);
        }
    }
}

