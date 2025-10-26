using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq.Query;
using CloudflareD1.NET.Linq.Mapping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class QueryBuilderTests : IDisposable
    {
        private readonly D1Client _client;
        private readonly Mock<ILogger<D1Client>> _mockLogger;

        public QueryBuilderTests()
        {
            _mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), _mockLogger.Object);

            // Set up test data
            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT,
                    is_active INTEGER DEFAULT 1
                )");

            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (1, 'Alice', 30, 'alice@example.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (2, 'Bob', 25, 'bob@example.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (3, 'Charlie', 35, 'charlie@example.com', 0)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (4, 'David', 28, 'david@example.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (5, 'Eve', 32, 'eve@example.com', 1)");
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string? Email { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public void Constructor_WithNullClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QueryBuilder<User>(null!, "users"));
        }

        [Fact]
        public void Constructor_WithNullTableName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QueryBuilder<User>(_client, null!));
        }

        [Fact]
        public async Task ToListAsync_WithoutConditions_ReturnsAllRowsAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCount(5);
        }

        [Fact]
        public async Task Where_SingleCondition_FiltersCorrectlyAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .Where("age > ?", 30)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(u => u.Age > 30);
        }

        [Fact]
        public async Task Where_MultipleConditions_ChainsCorrectlyAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .Where("age >= ?", 25)
                .Where("age <= ?", 32)
                .Where("is_active = ?", 1)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(4); // Alice(30), Bob(25), David(28), Eve(32)
            results.Should().OnlyContain(u => u.Age >= 25 && u.Age <= 32 && u.IsActive);
        }

        [Fact]
        public void Where_WithEmptyClause_ThrowsArgumentException()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.Where(""));
        }

        [Fact]
        public async Task OrderBy_SingleColumn_OrdersAscendingAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .OrderBy("age")
                .ToListAsync();

            // Assert
            var resultsList = results.ToList();
            resultsList.Should().HaveCount(5);
            resultsList[0].Age.Should().Be(25);
            resultsList[1].Age.Should().Be(28);
            resultsList[2].Age.Should().Be(30);
            resultsList[3].Age.Should().Be(32);
            resultsList[4].Age.Should().Be(35);
        }

        [Fact]
        public async Task OrderByDescending_SingleColumn_OrdersDescendingAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .OrderByDescending("age")
                .ToListAsync();

            // Assert
            var resultsList = results.ToList();
            resultsList.Should().HaveCount(5);
            resultsList[0].Age.Should().Be(35);
            resultsList[1].Age.Should().Be(32);
            resultsList[2].Age.Should().Be(30);
            resultsList[3].Age.Should().Be(28);
            resultsList[4].Age.Should().Be(25);
        }

        [Fact]
        public async Task ThenBy_AfterOrderBy_AppliesSecondaryOrderingAsync()
        {
            // Arrange
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (6, 'Frank', 30, 'frank@example.com', 1)");
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .OrderBy("age")
                .ThenBy("name")
                .ToListAsync();

            // Assert
            var resultsList = results.ToList();
            var age30Users = resultsList.Where(u => u.Age == 30).ToList();
            age30Users.Should().HaveCount(2);
            age30Users[0].Name.Should().Be("Alice");
            age30Users[1].Name.Should().Be("Frank");
        }

        [Fact]
        public void ThenBy_WithoutPriorOrderBy_ThrowsInvalidOperationException()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => query.ThenBy("name"));
        }

        [Fact]
        public void ThenByDescending_WithoutPriorOrderBy_ThrowsInvalidOperationException()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => query.ThenByDescending("name"));
        }

        [Fact]
        public async Task Take_LimitsResultsAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .OrderBy("id")
                .Take(3)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(3);
            var resultsList = results.ToList();
            resultsList[0].Id.Should().Be(1);
            resultsList[1].Id.Should().Be(2);
            resultsList[2].Id.Should().Be(3);
        }

        [Fact]
        public void Take_WithZeroOrNegative_ThrowsArgumentException()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.Take(0));
            Assert.Throws<ArgumentException>(() => query.Take(-1));
        }

        [Fact]
        public async Task Skip_SkipsResultsAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .OrderBy("id")
                .Skip(2)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(3);
            var resultsList = results.ToList();
            resultsList[0].Id.Should().Be(3);
            resultsList[1].Id.Should().Be(4);
            resultsList[2].Id.Should().Be(5);
        }

        [Fact]
        public void Skip_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => query.Skip(-1));
        }

        [Fact]
        public async Task TakeAndSkip_CombinedForPagination_WorksCorrectlyAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act - Page 2 with page size 2
            var results = await query
                .OrderBy("id")
                .Skip(2)
                .Take(2)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            var resultsList = results.ToList();
            resultsList[0].Id.Should().Be(3);
            resultsList[1].Id.Should().Be(4);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_WithResults_ReturnsFirstAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var result = await query
                .OrderBy("age")
                .FirstOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Age.Should().Be(25);
            result.Name.Should().Be("Bob");
        }

        [Fact]
        public async Task FirstOrDefaultAsync_WithNoResults_ReturnsNullAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var result = await query
                .Where("age > ?", 100)
                .FirstOrDefaultAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SingleAsync_WithOneResult_ReturnsSingleAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var result = await query
                .Where("id = ?", 1)
                .SingleAsync();

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Alice");
        }

        [Fact]
        public async Task SingleAsync_WithNoResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await query.Where("age > ?", 100).SingleAsync());
        }

        [Fact]
        public async Task SingleAsync_WithMultipleResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await query.Where("age > ?", 25).SingleAsync());
        }

        [Fact]
        public async Task SingleOrDefaultAsync_WithOneResult_ReturnsSingleAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var result = await query
                .Where("id = ?", 1)
                .SingleOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
        }

        [Fact]
        public async Task SingleOrDefaultAsync_WithNoResults_ReturnsNullAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var result = await query
                .Where("age > ?", 100)
                .SingleOrDefaultAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SingleOrDefaultAsync_WithMultipleResults_ThrowsInvalidOperationExceptionAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await query.Where("age > ?", 25).SingleOrDefaultAsync());
        }

        [Fact]
        public async Task CountAsync_ReturnsCorrectCountAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var count = await query
                .Where("is_active = ?", 1)
                .CountAsync();

            // Assert
            count.Should().Be(4);
        }

        [Fact]
        public async Task AnyAsync_WithResults_ReturnsTrueAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var exists = await query
                .Where("age > ?", 30)
                .AnyAsync();

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task AnyAsync_WithNoResults_ReturnsFalseAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var exists = await query
                .Where("age > ?", 100)
                .AnyAsync();

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ComplexQuery_WithMultipleClauses_WorksCorrectlyAsync()
        {
            // Arrange
            var query = new QueryBuilder<User>(_client, "users");

            // Act
            var results = await query
                .Where("is_active = ?", 1)
                .Where("age >= ?", 28)
                .OrderByDescending("age")
                .ThenBy("name")
                .Skip(1)
                .Take(2)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            var resultsList = results.ToList();
            resultsList[0].Age.Should().BeGreaterThanOrEqualTo(28);
            resultsList[0].IsActive.Should().BeTrue();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
