using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Exceptions;
using CloudflareD1.NET.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class D1ClientTests : IDisposable
    {
        private readonly Mock<ILogger<D1Client>> _mockLogger;
        private readonly D1Options _localOptions;
        private readonly D1Options _remoteOptions;

        public D1ClientTests()
        {
            _mockLogger = new Mock<ILogger<D1Client>>();

            _localOptions = new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = ":memory:"
            };

            _remoteOptions = new D1Options
            {
                UseLocalMode = false,
                AccountId = "test-account-id",
                DatabaseId = "test-database-id",
                ApiToken = "test-api-token"
            };
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new D1Client(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var options = Options.Create(_localOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new D1Client(options, null!));
        }

        [Fact]
        public void Constructor_WithInvalidConfiguration_ThrowsD1ConfigurationException()
        {
            // Arrange
            var invalidOptions = new D1Options
            {
                UseLocalMode = false
                // Missing required fields for remote mode
            };

            var options = Options.Create(invalidOptions);

            // Act & Assert
            Assert.Throws<D1ConfigurationException>(() =>
                new D1Client(options, _mockLogger.Object));
        }

        [Fact]
        public async Task QueryAsync_WithNullSql_ThrowsArgumentExceptionAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.QueryAsync(null!));
        }

        [Fact]
        public async Task QueryAsync_WithEmptySql_ThrowsArgumentExceptionAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.QueryAsync("   "));
        }

        [Fact]
        public async Task QueryAsync_LocalMode_ExecutesSuccessfullyAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Create a test table
            await client.ExecuteAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)");
            await client.ExecuteAsync("INSERT INTO users (id, name) VALUES (1, 'Alice')");

            // Act
            var result = await client.QueryAsync("SELECT * FROM users WHERE id = ?", new object[] { 1 });

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Results.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
            result.Results![0]["id"].Should().Be(1L);
            result.Results![0]["name"].Should().Be("Alice");
        }

        [Fact]
        public async Task ExecuteAsync_WithNullSql_ThrowsArgumentExceptionAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.ExecuteAsync(null!));
        }

        [Fact]
        public async Task ExecuteAsync_LocalMode_ExecutesSuccessfullyAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act
            var createResult = await client.ExecuteAsync(
                "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price REAL)");

            var insertResult = await client.ExecuteAsync(
                "INSERT INTO products (name, price) VALUES (?, ?)",
                new object[] { "Widget", 9.99 });

            // Assert
            createResult.Should().NotBeNull();
            createResult.Success.Should().BeTrue();

            insertResult.Should().NotBeNull();
            insertResult.Success.Should().BeTrue();
            insertResult.Meta.Should().NotBeNull();
            insertResult.Meta!.Changes.Should().Be(1);
        }

        [Fact]
        public async Task BatchAsync_WithNullStatements_ThrowsArgumentNullExceptionAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await client.BatchAsync(null!));
        }

        [Fact]
        public async Task BatchAsync_WithEmptyStatements_ThrowsArgumentExceptionAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.BatchAsync(new List<D1Statement>()));
        }

        [Fact]
        public async Task BatchAsync_LocalMode_ExecutesMultipleStatementsAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            await client.ExecuteAsync("CREATE TABLE orders (id INTEGER PRIMARY KEY, total REAL)");

            var statements = new List<D1Statement>
            {
                new D1Statement { Sql = "INSERT INTO orders (total) VALUES (?)", Params = new object[] { 100.50 } },
                new D1Statement { Sql = "INSERT INTO orders (total) VALUES (?)", Params = new object[] { 200.75 } },
                new D1Statement { Sql = "INSERT INTO orders (total) VALUES (?)", Params = new object[] { 50.25 } }
            };

            // Act
            var results = await client.BatchAsync(statements);

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCount(3);
            results.Should().OnlyContain(r => r.Success);
            results.Should().OnlyContain(r => r.Meta!.Changes == 1);
        }

        [Fact]
        public async Task QueryAsync_WithCancellationToken_CanBeCancelledAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);
            using var cts = new CancellationTokenSource();

            // Act
            cts.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await client.QueryAsync("SELECT * FROM sqlite_master", cancellationToken: cts.Token));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            // Act & Assert - should not throw
            client.Dispose();
            client.Dispose();
        }

        [Fact]
        public async Task QueryAsync_AfterDispose_ThrowsObjectDisposedExceptionAsync()
        {
            // Arrange
            var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);
            client.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await client.QueryAsync("SELECT 1"));
        }

        [Fact]
        public async Task ExecuteAsync_AfterDispose_ThrowsObjectDisposedExceptionAsync()
        {
            // Arrange
            var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);
            client.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await client.ExecuteAsync("CREATE TABLE test (id INTEGER)"));
        }

        [Fact]
        public async Task QueryAsync_LocalMode_WithComplexParametersAsync()
        {
            // Arrange
            using var client = new D1Client(Options.Create(_localOptions), _mockLogger.Object);

            await client.ExecuteAsync(@"
                CREATE TABLE complex_data (
                    id INTEGER PRIMARY KEY,
                    name TEXT,
                    age INTEGER,
                    salary REAL,
                    active INTEGER
                )");

            await client.ExecuteAsync(
                "INSERT INTO complex_data (name, age, salary, active) VALUES (?, ?, ?, ?)",
                new object[] { "Bob", 30, 75000.50, 1 });

            // Act
            var result = await client.QueryAsync(
                "SELECT * FROM complex_data WHERE age >= ? AND salary <= ? AND active = ?",
                new object[] { 25, 80000.0, 1 });

            // Assert
            result.Success.Should().BeTrue();
            result.Results.Should().HaveCount(1);
            result.Results![0]["name"].Should().Be("Bob");
            result.Results![0]["age"].Should().Be(30L);
            result.Results![0]["salary"].Should().Be(75000.50);
            result.Results![0]["active"].Should().Be(1L);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
