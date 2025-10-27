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

namespace CloudflareD1.NET.Linq.Tests;

public class AnyAllTests : IDisposable
{
    private readonly D1Client _client;

    public AnyAllTests()
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
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    [Fact]
    public async Task AnyAsync_WithPredicate_ReturnsTrue_WhenMatchExists()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if any users are over 25
        var result = await queryBuilder.AnyAsync(u => u.Age > 25);

        // Assert
        Assert.True(result); // Alice (30), Charlie (35), David (28)
    }

    [Fact]
    public async Task AnyAsync_WithPredicate_ReturnsFalse_WhenNoMatchExists()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if any users are over 100
        var result = await queryBuilder.AnyAsync(u => u.Age > 100);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AnyAsync_WithPredicate_CombinesWithExistingWhere()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.IsActive);

        // Act - check if any active users are over 25
        var result = await queryBuilder.AnyAsync(u => u.Age > 25);

        // Assert
        Assert.True(result); // Alice (30, active), David (28, active)
    }

    [Fact]
    public async Task AnyAsync_WithPredicate_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            queryBuilder.AnyAsync(null!));
    }

    [Fact]
    public async Task AllAsync_WithPredicate_ReturnsTrue_WhenAllMatch()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if all users are over 18
        var result = await queryBuilder.AllAsync(u => u.Age > 18);

        // Assert
        Assert.True(result); // All users: Alice (30), Bob (25), Charlie (35), David (28), Eve (22)
    }

    [Fact]
    public async Task AllAsync_WithPredicate_ReturnsFalse_WhenAnyDoNotMatch()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if all users are over 30
        var result = await queryBuilder.AllAsync(u => u.Age > 30);

        // Assert
        Assert.False(result); // Bob (25), David (28), Eve (22) are not over 30
    }

    [Fact]
    public async Task AllAsync_WithPredicate_CombinesWithExistingWhere()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.IsActive);

        // Act - check if all active users are over 20
        var result = await queryBuilder.AllAsync(u => u.Age > 20);

        // Assert
        Assert.True(result); // Active: Alice (30), Bob (25), David (28)
    }

    [Fact]
    public async Task AllAsync_WithPredicate_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            queryBuilder.AllAsync(null!));
    }

    [Fact]
    public async Task AnyAsync_WithComplexPredicate_GeneratesCorrectSQL()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if any users are over 25 AND active
        var result = await queryBuilder.AnyAsync(u => u.Age > 25 && u.IsActive);

        // Assert
        Assert.True(result); // Alice (30, active), David (28, active)
    }

    [Fact]
    public async Task AllAsync_WithComplexPredicate_GeneratesCorrectSQL()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if all users are either over 20 OR inactive
        var result = await queryBuilder.AllAsync(u => u.Age > 20 || !u.IsActive);

        // Assert
        Assert.True(result); // All users meet at least one condition
    }

    [Fact]
    public async Task AnyAsync_WithStringComparison_GeneratesCorrectSQL()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act - check if any user named "Alice" exists
        var result = await queryBuilder.AnyAsync(u => u.Name == "Alice");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AllAsync_WithEqualityCheck_GeneratesCorrectSQL()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.Age > 30);

        // Act - check if all users over 30 are inactive
        var result = await queryBuilder.AllAsync(u => !u.IsActive);

        // Assert
        Assert.True(result); // Only Charlie (35, inactive) is over 30
    }
}
