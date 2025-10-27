using System;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    /// <summary>
    /// Tests for database transaction functionality.
    /// </summary>
    public class TransactionTests : IDisposable
    {
        private readonly D1Client _client;

        public TransactionTests()
        {
            var mockLogger = new Mock<ILogger<D1Client>>();
            var options = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };
            _client = new D1Client(Options.Create(options), mockLogger.Object);

            // Setup test table
            SetupTestTableAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestTableAsync()
        {
            await _client.ExecuteAsync(@"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    balance INTEGER DEFAULT 0
                );
            ");

            // Insert test data
            await _client.ExecuteAsync("INSERT INTO test_users (name, email, balance) VALUES ('Alice', 'alice@test.com', 1000)");
            await _client.ExecuteAsync("INSERT INTO test_users (name, email, balance) VALUES ('Bob', 'bob@test.com', 500)");
        }

        [Fact]
        public async Task BeginTransactionAsync_ReturnsActiveTransaction()
        {
            // Act
            var transaction = await _client.BeginTransactionAsync();

            // Assert
            Assert.NotNull(transaction);
            Assert.True(transaction.IsActive);
        }

        [Fact]
        public async Task Transaction_Commit_ExecutesAllStatements()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();

            // Act
            await transaction.ExecuteAsync("INSERT INTO test_users (name, email, balance) VALUES (?, ?, ?)",
                new object[] { "Charlie", "charlie@test.com", 750 });
            await transaction.ExecuteAsync("UPDATE test_users SET balance = balance + 100 WHERE name = ?",
                new object[] { "Alice" });
            await transaction.CommitAsync();

            // Assert
            var result = await _client.QueryAsync("SELECT * FROM test_users WHERE name = 'Charlie'");
            Assert.Single(result.Results!);

            var aliceResult = await _client.QueryAsync("SELECT balance FROM test_users WHERE name = 'Alice'");
            Assert.Equal(1100L, aliceResult.Results![0]["balance"]);

            Assert.False(transaction.IsActive);
        }

        [Fact]
        public async Task Transaction_Rollback_DiscardsChanges()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();

            // Act
            await transaction.ExecuteAsync("INSERT INTO test_users (name, email, balance) VALUES (?, ?, ?)",
                new object[] { "Dave", "dave@test.com", 300 });
            await transaction.RollbackAsync();

            // Assert
            var result = await _client.QueryAsync("SELECT * FROM test_users WHERE name = 'Dave'");
            Assert.Empty(result.Results!);
            Assert.False(transaction.IsActive);
        }

        [Fact]
        public async Task Transaction_Dispose_AutoRollsBack()
        {
            // Act
            await using (var transaction = await _client.BeginTransactionAsync())
            {
                await transaction.ExecuteAsync("INSERT INTO test_users (name, email, balance) VALUES (?, ?, ?)",
                    new object[] { "Eve", "eve@test.com", 200 });
                // Dispose without commit
            }

            // Assert
            var result = await _client.QueryAsync("SELECT * FROM test_users WHERE name = 'Eve'");
            Assert.Empty(result.Results!);
        }

        [Fact]
        public async Task Transaction_CommitTwice_ThrowsException()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task Transaction_RollbackAfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        }

        [Fact]
        public async Task Transaction_ExecuteAfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                transaction.ExecuteAsync("INSERT INTO test_users (name, email) VALUES ('Test', 'test@test.com')"));
        }

        [Fact]
        public async Task Transaction_QueryAsync_AddsToTransaction()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();

            // Act
            await transaction.QueryAsync("SELECT * FROM test_users WHERE name = ?", new object[] { "Alice" });
            await transaction.ExecuteAsync("UPDATE test_users SET balance = 2000 WHERE name = ?", new object[] { "Alice" });
            await transaction.CommitAsync();

            // Assert
            var result = await _client.QueryAsync("SELECT balance FROM test_users WHERE name = 'Alice'");
            Assert.Equal(2000L, result.Results![0]["balance"]);
        }

        [Fact]
        public async Task Transaction_EmptyCommit_Succeeds()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();

            // Act & Assert
            await transaction.CommitAsync(); // Should not throw
            Assert.False(transaction.IsActive);
        }

        [Fact]
        public async Task Transaction_MultipleStatements_AllOrNothing()
        {
            // Arrange
            var transaction = await _client.BeginTransactionAsync();

            // Act
            await transaction.ExecuteAsync("UPDATE test_users SET balance = 1500 WHERE name = 'Alice'");
            await transaction.ExecuteAsync("UPDATE test_users SET balance = 1000 WHERE name = 'Bob'");
            
            // Simulate error by rolling back
            await transaction.RollbackAsync();

            // Assert - Both updates should be rolled back
            var aliceResult = await _client.QueryAsync("SELECT balance FROM test_users WHERE name = 'Alice'");
            Assert.Equal(1000L, aliceResult.Results![0]["balance"]); // Original value

            var bobResult = await _client.QueryAsync("SELECT balance FROM test_users WHERE name = 'Bob'");
            Assert.Equal(500L, bobResult.Results![0]["balance"]); // Original value
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
