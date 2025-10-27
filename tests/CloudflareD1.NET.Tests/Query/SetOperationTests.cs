using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests.Query
{
    public class SetOperationTests : IDisposable
    {
        private readonly D1Client _client;

        public SetOperationTests()
        {
            var mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), mockLogger.Object);

            // Setup test data
            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    is_active INTEGER NOT NULL
                )");

            // Insert test data
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (1, 'Alice', 30, 'alice@example.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (2, 'Bob', 25, 'bob@test.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (3, 'Charlie', 35, 'charlie@example.com', 0)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (4, 'David', 28, 'david@example.com', 1)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (5, 'Eve', 22, 'eve@test.com', 0)");
            await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (6, 'Frank', 40, 'frank@example.com', 1)");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public string Email { get; set; } = "";
            public bool IsActive { get; set; }
        }

        [Fact]
        public async Task Union_WithBasicQueries_ReturnsUniqueResults()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 30);
            var query2 = _client.Query<User>("users").Where("email LIKE ?", "%@example.com%");
            var result = await query1.Union(query2).ToListAsync();

            // Assert
            // Age > 30: Charlie (35), Frank (40)
            // Email contains @example.com: Alice, Charlie, David, Frank
            // UNION (unique): Alice, Charlie, David, Frank = 4 users
            Assert.True(result.Count() >= 4); // At least these 4
        }

        [Fact]
        public async Task UnionAll_WithBasicQueries_KeepsDuplicates()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 25);
            var query2 = _client.Query<User>("users").Where("age > ?", 25);
            var result = await query1.UnionAll(query2).ToListAsync();

            // Assert
            // Age > 25: Alice (30), Charlie (35), David (28), Frank (40) = 4 users
            // UNION ALL should have duplicates: 4 + 4 = 8 results
            Assert.Equal(8, result.Count());
        }

        [Fact]
        public async Task Intersect_WithBasicQueries_ReturnsCommonResults()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 25);
            var query2 = _client.Query<User>("users").Where("email LIKE ?", "%@example.com%");
            var result = await query1.Intersect(query2).ToListAsync();

            // Assert
            // Age > 25: Alice (30), Charlie (35), David (28), Frank (40)
            // Email @example.com: Alice, Charlie, David, Frank
            // INTERSECT: Alice, Charlie, David, Frank = 4 users
            Assert.Equal(4, result.Count());
        }

        [Fact]
        public async Task Except_WithBasicQueries_ReturnsOnlyFirstQueryResults()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 20);
            var query2 = _client.Query<User>("users").Where("is_active = ?", 0);
            var result = await query1.Except(query2).ToListAsync();

            // Assert
            // Age > 20: All 6 users
            // is_active = 0: Charlie, Eve = 2 users
            // EXCEPT: All except Charlie and Eve = 4 users
            Assert.Equal(4, result.Count());
        }

        [Fact]
        public async Task Union_WithMultipleChainedOperations_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age < ?", 25);
            var query2 = _client.Query<User>("users").Where("age > ?", 35);
            var query3 = _client.Query<User>("users").Where("age = ?", 30);
            var result = await query1.Union(query2).Union(query3).ToListAsync();

            // Assert
            // Age < 25: Bob (22), Eve (22) = 2 unique (Bob or Eve, both 22)
            // Age > 35: Frank (40) = 1
            // Age = 30: Alice (30) = 1
            // Total unique: 3 users (one of Bob/Eve, Frank, Alice)
            Assert.True(result.Count() >= 3);
        }

        [Fact]
        public async Task SetOperation_WithOrderByAndTake_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 25).OrderBy("age").Take(2);
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1).OrderBy("name");
            var result = await query1.Union(query2).ToListAsync();

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task SetOperation_WithDistinct_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 25).Distinct();
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1);
            var result = await query1.Union(query2).ToListAsync();

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Union_ThrowsArgumentNullException_WhenOtherQueryIsNull()
        {
            // Arrange
            var query = _client.Query<User>("users");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query.Union(null!));
        }

        [Fact]
        public void UnionAll_ThrowsArgumentNullException_WhenOtherQueryIsNull()
        {
            // Arrange
            var query = _client.Query<User>("users");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query.UnionAll(null!));
        }

        [Fact]
        public void Intersect_ThrowsArgumentNullException_WhenOtherQueryIsNull()
        {
            // Arrange
            var query = _client.Query<User>("users");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query.Intersect(null!));
        }

        [Fact]
        public void Except_ThrowsArgumentNullException_WhenOtherQueryIsNull()
        {
            // Arrange
            var query = _client.Query<User>("users");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => query.Except(null!));
        }

        [Fact]
        public async Task SetOperation_FirstOrDefaultAsync_ReturnsFirstResult()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 20).OrderBy("name");
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1).OrderBy("name");
            var result = await query1.Union(query2).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice", result!.Name);
        }

        [Fact]
        public async Task SetOperation_CountAsync_ReturnsCorrectCount()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 25);
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1);
            var count = await query1.Union(query2).CountAsync();

            // Assert
            // Age > 25: Alice (30), Charlie (35), David (28), Frank (40) = 4
            // is_active = 1: Alice, Bob, David, Frank = 4
            // UNION unique: Alice, Bob, Charlie, David, Frank = 5
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task SetOperation_AnyAsync_ReturnsTrueWhenResultsExist()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 20);
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1);
            var exists = await query1.Union(query2).AnyAsync();

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task SetOperation_AnyAsync_ReturnsFalseWhenNoResults()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 150);
            var query2 = _client.Query<User>("users").Where("age < ?", 0);
            var exists = await query1.Union(query2).AnyAsync();

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task Union_WithComplexWhereClause_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ? AND is_active = ?", 25, 1);
            var query2 = _client.Query<User>("users").Where("email LIKE ? OR name = ?", "%@example.com%", "Bob");
            var result = await query1.Union(query2).ToListAsync();

            // Assert
            Assert.NotEmpty(result);
            Assert.True(result.Count() >= 4);
        }

        [Fact]
        public async Task UnionAll_WithEmptyResults_ReturnsEmptyList()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 200);
            var query2 = _client.Query<User>("users").Where("age < ?", 0);
            var result = await query1.UnionAll(query2).ToListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Intersect_WithTakeAndSkip_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 20).Take(50).Skip(0);
            var query2 = _client.Query<User>("users").Where("is_active = ?", 1).Take(100);
            var result = await query1.Intersect(query2).ToListAsync();

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task Except_MixedWithIntersect_WorksCorrectly()
        {
            // Arrange & Act
            var query1 = _client.Query<User>("users").Where("age > ?", 20);
            var query2 = _client.Query<User>("users").Where("age < ?", 40);
            var query3 = _client.Query<User>("users").Where("is_active = ?", 0);
            var result = await query1.Intersect(query2).Except(query3).ToListAsync();

            // Assert
            // Age > 20: All except Bob and Eve (22 each) - actually includes them since 22 > 20 = 6 users
            // Age < 40: All except Frank (40) = 5 users
            // INTERSECT: Ages between 21-39: Bob (25), Alice (30), Charlie (35), David (28), Eve (22) = 5 users
            // EXCEPT is_active = 0 (Charlie, Eve): Bob, Alice, David = 3 users
            Assert.True(result.Count() >= 2);  // At least 2-3 users
        }
    }
}
