using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq.Query;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    /// <summary>
    /// Unit tests for Select() with computed properties.
    /// </summary>
    public class SelectComputedPropertiesTests
    {
        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        private class UserWithComputed
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsAdult { get; set; }
        }

        private class UserWithMath
        {
            public int Id { get; set; }
            public decimal Total { get; set; }
        }

        [Fact]
        public async Task Select_ComputedBooleanProperty_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var mockResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John", ["IsAdult"] = true }
                }
            };

            mockClient
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("(age >= ?) AS IsAdult")),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var projectionBuilder = queryBuilder.Select(u => new UserWithComputed
            {
                Id = u.Id,
                Name = u.Name,
                IsAdult = u.Age >= 18
            });

            var result = await projectionBuilder.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            var first = result.First();
            first.Id.Should().Be(1);
            first.Name.Should().Be("John");
            first.IsAdult.Should().BeTrue();

            mockClient.Verify(x => x.QueryAsync(
                It.Is<string>(sql => sql.Contains("id AS Id") &&
                                     sql.Contains("name AS Name") &&
                                     sql.Contains("(age >= ?) AS IsAdult") &&
                                     sql.Contains("FROM users")),
                It.Is<object[]>(p => p != null && p.Length == 1 && (int)p[0] == 18),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Select_ComputedMathProperty_GeneratesCorrectSQL()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var mockResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Total"] = 100.50m }
                }
            };

            mockClient
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("(price * quantity) AS Total")),
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var projectionBuilder = queryBuilder.Select(u => new UserWithMath
            {
                Id = u.Id,
                Total = u.Price * u.Quantity
            });

            var result = await projectionBuilder.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            var first = result.First();
            first.Id.Should().Be(1);
            first.Total.Should().Be(100.50m);

            mockClient.Verify(x => x.QueryAsync(
                It.Is<string>(sql => sql.Contains("id AS Id") &&
                                     sql.Contains("(price * quantity) AS Total") &&
                                     sql.Contains("FROM users")),
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Select_AnonymousTypeWithComputedProperty_GeneratesCorrectSQL()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var mockResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>()
            };

            var mockClient = new Mock<ID1Client>();
            mockClient
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            // Act - Using anonymous type (won't work with ToListAsync, but we can test SQL generation)
            var selectExpression = (Expression<Func<User, object>>)(u => new
            {
                u.Id,
                u.Name,
                IsAdult = u.Age >= 18,
                Total = u.Price * u.Quantity
            });

            var visitor = new SelectExpressionVisitor(mapper);
            var columns = visitor.GetColumns(selectExpression.Body);

            // Assert
            columns.Should().HaveCount(4);
            columns[0].Should().Be(("id", "Id"));
            columns[1].Should().Be(("name", "Name"));
            columns[2].Column.Should().Contain("age >= ?");
            columns[2].Alias.Should().Be("IsAdult");
            columns[3].Column.Should().Contain("price * quantity");
            columns[3].Alias.Should().Be("Total");
        }

        [Fact]
        public void Select_ComputedWithStringMethod_GeneratesCorrectSQL()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var selectExpression = (Expression<Func<User, object>>)(u => new
            {
                u.Id,
                UpperName = u.Name.ToUpper()
            });

            var visitor = new SelectExpressionVisitor(mapper);

            // Act
            var columns = visitor.GetColumns(selectExpression.Body);

            // Assert
            columns.Should().HaveCount(2);
            columns[0].Should().Be(("id", "Id"));
            columns[1].Column.Should().Be("UPPER(name)");
            columns[1].Alias.Should().Be("UpperName");
        }

        [Fact]
        public void Select_ComputedWithComparison_GeneratesCorrectSQL()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var selectExpression = (Expression<Func<User, object>>)(u => new
            {
                u.Id,
                IsExpensive = u.Price > 100
            });

            var visitor = new SelectExpressionVisitor(mapper);

            // Act
            var columns = visitor.GetColumns(selectExpression.Body);

            // Assert
            columns.Should().HaveCount(2);
            columns[0].Should().Be(("id", "Id"));
            columns[1].Column.Should().Contain("price > ?");
            columns[1].Alias.Should().Be("IsExpensive");
        }

        [Fact]
        public void Select_MultipleComputedProperties_GeneratesCorrectSQL()
        {
            // Arrange
            var mapper = new DefaultEntityMapper();
            var selectExpression = (Expression<Func<User, object>>)(u => new
            {
                u.Id,
                IsAdult = u.Age >= 18,
                IsMinor = u.Age < 18,
                Total = u.Price * u.Quantity,
                Discount = u.Price * 0.1m
            });

            var visitor = new SelectExpressionVisitor(mapper);

            // Act
            var columns = visitor.GetColumns(selectExpression.Body);

            // Assert
            columns.Should().HaveCount(5);
            columns[0].Should().Be(("id", "Id"));
            columns[1].Column.Should().Contain("age >= ?");
            columns[1].Alias.Should().Be("IsAdult");
            columns[2].Column.Should().Contain("age < ?");
            columns[2].Alias.Should().Be("IsMinor");
            columns[3].Column.Should().Contain("price * quantity");
            columns[3].Alias.Should().Be("Total");
            columns[4].Column.Should().Contain("price * ?");
            columns[4].Alias.Should().Be("Discount");
        }
    }
}
