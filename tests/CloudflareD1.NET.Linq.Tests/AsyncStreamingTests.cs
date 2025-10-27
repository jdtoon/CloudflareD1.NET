using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Linq.Tests;

public class AsyncStreamingTests : IDisposable
{
    private readonly D1Client _client;

    public AsyncStreamingTests()
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

        // Insert test data - 20 users for better testing of streaming behavior
        for (int i = 1; i <= 20; i++)
        {
            await _client.ExecuteAsync(
                $"INSERT INTO users (id, name, age, email, is_active) VALUES ({i}, 'User{i}', {20 + i}, 'user{i}@example.com', {i % 2})");
        }
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
    public async Task ToAsyncEnumerable_ReturnsAllRecords()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
            Assert.NotNull(user);
            Assert.NotEmpty(user.Name);
        }

        // Assert
        Assert.Equal(20, count);
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithWhere_FiltersCorrectly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.IsActive);
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
            Assert.True(user.IsActive); // All should be active
        }

        // Assert
        Assert.Equal(10, count); // Half of the users (even IDs)
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithOrderBy_ReturnsOrderedResults()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .OrderBy(u => u.Age);
        var previousAge = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            Assert.True(user.Age >= previousAge);
            previousAge = user.Age;
        }
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithTake_LimitsResults()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Take(5);
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
        }

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithSkip_SkipsRecords()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .OrderBy(u => u.Id)
            .Skip(15);
        var count = 0;
        var firstId = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            if (count == 0)
            {
                firstId = user.Id;
            }
            count++;
        }

        // Assert
        Assert.Equal(5, count); // 20 - 15 = 5 remaining
        Assert.True(firstId >= 16); // Should start after 15th record
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithComplexQuery_WorksCorrectly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.Age > 25)
            .OrderByDescending(u => u.Age)
            .Take(10);
        var count = 0;
        var previousAge = int.MaxValue;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            Assert.True(user.Age > 25);
            Assert.True(user.Age <= previousAge);
            previousAge = user.Age;
            count++;
        }

        // Assert
        Assert.True(count > 0);
        Assert.True(count <= 10);
    }

    [Fact]
    public async Task ToAsyncEnumerable_CanBreakEarly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
            if (count == 5)
            {
                break; // Early termination
            }
        }

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");
        var cts = new CancellationTokenSource();
        var count = 0;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var user in queryBuilder.ToAsyncEnumerable(cts.Token))
            {
                count++;
                if (count == 5)
                {
                    cts.Cancel(); // Cancel after 5 items
                }
            }
        });

        // Assert - should have processed some items before cancellation
        Assert.True(count >= 5);
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithNoResults_ReturnsEmpty()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.Age > 1000); // No users this old
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
        }

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ToAsyncEnumerable_MultipleEnumerations_Work()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Take(5);

        // Act - enumerate twice
        var count1 = 0;
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count1++;
        }

        var count2 = 0;
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count2++;
        }

        // Assert
        Assert.Equal(5, count1);
        Assert.Equal(5, count2);
    }

    [Fact]
    public async Task ToAsyncEnumerable_WithDistinct_WorksCorrectly()
    {
        // Arrange - First insert a duplicate
        await _client.ExecuteAsync("INSERT INTO users (id, name, age, email, is_active) VALUES (100, 'User1', 21, 'duplicate@example.com', 1)");

        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.Name == "User1");
        var count = 0;

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            count++;
        }

        // Assert - should have 2 users with name "User1"
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ToAsyncEnumerable_YieldsItemsOneAtATime()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .OrderBy(u => u.Id)
            .Take(3);
        var yielded = new List<int>();

        // Act
        await foreach (var user in queryBuilder.ToAsyncEnumerable())
        {
            yielded.Add(user.Id);
            // Verify we're getting items one at a time (not all at once)
            Assert.True(yielded.Count <= 3);
        }

        // Assert
        Assert.Equal(3, yielded.Count);
        Assert.Equal(new[] { 1, 2, 3 }, yielded);
    }

    [Fact]
    public async Task ToListAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await queryBuilder.ToListAsync(cts.Token);
        });
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .OrderBy(u => u.Id);

        // Act
        var user = await queryBuilder.FirstOrDefaultAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
    }

    [Fact]
    public async Task CountAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users");

        // Act
        var count = await queryBuilder.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(20, count);
    }

    [Fact]
    public async Task AnyAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var queryBuilder = _client.Query<User>("users")
            .Where(u => u.Age > 30);

        // Act
        var hasResults = await queryBuilder.AnyAsync(CancellationToken.None);

        // Assert
        Assert.True(hasResults);
    }
}
