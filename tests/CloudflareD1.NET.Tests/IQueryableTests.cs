using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using CloudflareD1.NET.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class IQueryableTests
    {
        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public async Task AsQueryable_CreatesIQueryableInstance()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");

            // Assert
            queryable.Should().NotBeNull();
            queryable.Should().BeAssignableTo<IQueryable<User>>();
            queryable.ElementType.Should().Be(typeof(User));
            queryable.Provider.Should().NotBeNull();
        }

        [Fact]
        public async Task IQueryable_DeferredExecution_DoesNotExecuteImmediately()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new D1QueryResult { Success = true, Results = new List<Dictionary<string, object?>>() });

            // Act - Create queryable and compose query
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.Where(u => u.Age >= 18).OrderBy(u => u.Name);

            // Assert - No query should have been executed yet
            mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task IQueryable_ToListAsync_ExecutesQuery()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John", ["Email"] = "john@test.com", ["Age"] = 25, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE") && sql.Contains("age >= ?")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.Where(u => u.Age >= 18);

            // Cast to D1Queryable to access ToListAsync
            var results = await ((D1Queryable<User>)query).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            results.First().Name.Should().Be("John");
            mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_MultipleWhere_ChainsProperly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John", ["Email"] = "john@test.com", ["Age"] = 25, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable
                .Where(u => u.Age >= 18)
                .Where(u => u.IsActive);

            var results = await ((D1Queryable<User>)query).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_OrderBy_AppliesOrdering()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "Alice", ["Email"] = "alice@test.com", ["Age"] = 25, ["IsActive"] = true },
                    new() { ["Id"] = 2, ["Name"] = "Bob", ["Email"] = "bob@test.com", ["Age"] = 30, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("ORDER BY name")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.OrderBy(u => u.Name);
            var results = await ((D1Queryable<User>)query).ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("ORDER BY")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_TakeAndSkip_AppliesPagination()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 2, ["Name"] = "Bob", ["Email"] = "bob@test.com", ["Age"] = 30, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("LIMIT 1 OFFSET 1")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.Skip(1).Take(1);
            var results = await ((D1Queryable<User>)query).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("LIMIT") && sql.Contains("OFFSET")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_ComplexQuery_CombinesAllOperations()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "Alice", ["Email"] = "alice@test.com", ["Age"] = 25, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql =>
                    sql.Contains("WHERE") &&
                    sql.Contains("ORDER BY") &&
                    sql.Contains("LIMIT") &&
                    sql.Contains("OFFSET")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable
                .Where(u => u.Age >= 18)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Skip(10)
                .Take(5);

            var results = await ((D1Queryable<User>)query).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE") && sql.Contains("ORDER BY")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_CountAsync_ExecutesCountQuery()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["COUNT(*)"] = 42 }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("SELECT COUNT(*)")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.Where(u => u.Age >= 18);
            var count = await ((D1Queryable<User>)query).CountAsync();

            // Assert
            count.Should().Be(42);
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("COUNT")),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_FirstOrDefaultAsync_ExecutesQuery()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John", ["Email"] = "john@test.com", ["Age"] = 25, ["IsActive"] = true }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable.Where(u => u.Age >= 18);
            var first = await ((D1Queryable<User>)query).FirstOrDefaultAsync();

            // Assert
            first.Should().NotBeNull();
            first!.Name.Should().Be("John");
        }

        [Fact]
        public void IQueryable_Provider_ReturnsD1QueryProvider()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");

            // Assert
            queryable.Provider.Should().BeOfType<D1QueryProvider>();
        }

        [Fact]
        public void IQueryable_Expression_IsNotNull()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");

            // Assert
            queryable.Expression.Should().NotBeNull();
        }
    }
}
