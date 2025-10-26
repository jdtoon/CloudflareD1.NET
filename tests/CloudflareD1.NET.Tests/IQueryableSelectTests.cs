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
    public class IQueryableSelectTests
    {
        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        private class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class UserSummary
        {
            public string DisplayName { get; set; } = string.Empty;
            public int YearsOld { get; set; }
        }

        [Fact]
        public async Task IQueryable_Select_SimpleProjection_ReturnsProjectedType()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John" },
                    new() { ["Id"] = 2, ["Name"] = "Jane" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("SELECT") && sql.Contains("FROM users")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable.Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = (await ((D1ProjectionQueryable<UserDto>)projectedQuery).ToListAsync()).ToList();

            // Assert
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(1);
            results[0].Name.Should().Be("John");
            results[1].Id.Should().Be(2);
            results[1].Name.Should().Be("Jane");
            mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IQueryable_Select_WithComputedProperties()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["DisplayName"] = "John", ["YearsOld"] = 25 }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable.Select(u => new UserSummary
            {
                DisplayName = u.Name,
                YearsOld = u.Age
            });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = await ((D1ProjectionQueryable<UserSummary>)projectedQuery).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            results.First().DisplayName.Should().Be("John");
            results.First().YearsOld.Should().Be(25);
        }

        [Fact]
        public async Task IQueryable_Select_AfterWhere_ChainsProperly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType() == typeof(object[]) && ((object[])p).Length == 1 && (int)((object[])p)[0] == 18),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable
                .Where(u => u.Age >= 18)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = await ((D1ProjectionQueryable<UserDto>)projectedQuery).ToListAsync();

            // Assert
            results.Should().HaveCount(1);
            results.First().Id.Should().Be(1);
            results.First().Name.Should().Be("John");
        }

        [Fact]
        public async Task IQueryable_Select_AfterOrderBy_ChainsProperly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "Alice" },
                    new() { ["Id"] = 2, ["Name"] = "Bob" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable
                .OrderBy(u => u.Name)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = (await ((D1ProjectionQueryable<UserDto>)projectedQuery).ToListAsync()).ToList();

            // Assert
            results.Should().HaveCount(2);
            results[0].Name.Should().Be("Alice");
            results[1].Name.Should().Be("Bob");
        }

        [Fact]
        public async Task IQueryable_Select_WithPagination_ChainsProperly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 6, ["Name"] = "User 6" },
                    new() { ["Id"] = 7, ["Name"] = "User 7" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable
                .Skip(5)
                .Take(5)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = (await ((D1ProjectionQueryable<UserDto>)projectedQuery).ToListAsync()).ToList();

            // Assert
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(6);
            results[1].Id.Should().Be(7);
        }

        [Fact]
        public async Task IQueryable_Select_ComplexChain_WhereOrderByTake()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 3, ["Name"] = "Charlie" },
                    new() { ["Id"] = 1, ["Name"] = "Alice" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable
                .Where(u => u.Age >= 18)
                .OrderBy(u => u.Name)
                .Take(2)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access ToListAsync
            var results = await ((D1ProjectionQueryable<UserDto>)projectedQuery).ToListAsync();

            // Assert
            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task IQueryable_Select_DeferredExecution_DoesNotExecuteImmediately()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new D1QueryResult { Success = true, Results = new List<Dictionary<string, object?>>() });

            // Act - Create queryable and compose query with Select
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var query = queryable
                .Where(u => u.Age >= 18)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Assert - No query should have been executed yet
            mockClient.Verify(c => c.QueryAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task IQueryable_Select_FirstOrDefaultAsync()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["Id"] = 1, ["Name"] = "John" }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("SELECT") && sql.Contains("FROM users")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable.Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access FirstOrDefaultAsync
            var result = await ((D1ProjectionQueryable<UserDto>)projectedQuery).FirstOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("John");
        }

        [Fact]
        public async Task IQueryable_Select_CountAsync()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["count"] = 5L }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable.Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access CountAsync
            var count = await ((D1ProjectionQueryable<UserDto>)projectedQuery).CountAsync();

            // Assert
            count.Should().Be(5);
        }

        [Fact]
        public async Task IQueryable_Select_AnyAsync()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new() { ["EXISTS"] = 1L }
                }
            };

            mockClient.Setup(c => c.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            // Act
            IQueryable<User> queryable = mockClient.Object.AsQueryable<User>("users");
            var projectedQuery = queryable.Select(u => new UserDto { Id = u.Id, Name = u.Name });

            // Cast to D1ProjectionQueryable to access AnyAsync
            var hasAny = await ((D1ProjectionQueryable<UserDto>)projectedQuery).AnyAsync();

            // Assert
            hasAny.Should().BeTrue();
        }
    }
}
