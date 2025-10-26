using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Linq.Query;
using CloudflareD1.NET.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class SelectProjectionTests
    {
        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        private class UserSummary
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task Select_SpecificColumns_ExecutesQueryAndMapsResults()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" } },
                    new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Jane" } }
                }
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("SELECT") && sql.Contains("id") && sql.Contains("name")),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var results = await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task Select_WithWhere_CombinesCorrectly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" } }
                }
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE") && sql.Contains("age >= ?")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var results = await queryBuilder
                .Where(u => u.Age >= 18)
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .ToListAsync();

            // Assert
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task Select_WithOrderBy_CombinesCorrectly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>()
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("ORDER BY")),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            await queryBuilder
                .OrderBy(u => u.Name)
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .ToListAsync();

            // Assert - verify SQL was called with ORDER BY
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("ORDER BY name ASC")),
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Select_WithTakeAndSkip_CombinesCorrectly()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>()
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("LIMIT") && sql.Contains("OFFSET")),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .Skip(10)
                .Take(5)
                .ToListAsync();

            // Assert
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("LIMIT 5") && sql.Contains("OFFSET 10")),
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Select_FirstOrDefault_ReturnsFirstResult()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" } }
                }
            };
            
            mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var result = await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .FirstOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("John");
        }

        [Fact]
        public async Task Select_Count_ReturnsCorrectCount()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "count", 42 } }
                }
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("COUNT(*)")),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var count = await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .CountAsync();

            // Assert
            count.Should().Be(42);
        }

        [Fact]
        public async Task Select_Any_ReturnsTrueWhenResultsExist()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" } }
                }
            };
            
            mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            var hasResults = await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .AnyAsync();

            // Assert
            hasResults.Should().BeTrue();
        }

        [Fact]
        public async Task ProjectionBuilder_Where_AddsWhereClause()
        {
            // Arrange
            var mockClient = new Mock<ID1Client>();
            var queryResult = new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>()
            };
            
            mockClient.Setup(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE") && sql.Contains("is_active = ?")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResult);

            var queryBuilder = new QueryBuilder<User>(mockClient.Object, "users");

            // Act
            await queryBuilder
                .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
                .Where("is_active = ?", true)
                .ToListAsync();

            // Assert - verify WHERE clause was added
            mockClient.Verify(c => c.QueryAsync(
                It.Is<string>(sql => sql.Contains("WHERE is_active = ?")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
