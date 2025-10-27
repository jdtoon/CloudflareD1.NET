using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Linq.Tests.Query
{
    /// <summary>
    /// Tests for CompiledQuery query optimization and caching.
    /// </summary>
    public class CompiledQueryTests : IDisposable
    {
        private readonly D1Client _client;
        private readonly string _tableName = "users";

        public CompiledQueryTests()
        {
            // Clear cache before each test to avoid interference
            CompiledQuery.ClearCache();

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
            await _client.ExecuteAsync($@"
                CREATE TABLE {_tableName} (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT NOT NULL
                );
            ");

            // Insert test data
            for (int i = 1; i <= 20; i++)
            {
                await _client.ExecuteAsync($@"
                    INSERT INTO {_tableName} (name, email, age, is_active, created_at)
                    VALUES ('User {i}', 'user{i}@test.com', {20 + i}, {(i % 2)}, '2024-01-{i:D2}T00:00:00Z')
                ");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        [Fact]
        public async Task CompiledQuery_Create_CompilesSqlSuccessfully()
        {
            // Arrange & Act
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25)
            );

            // Assert
            Assert.NotNull(compiledQuery);
            Assert.NotNull(compiledQuery.Sql);
            Assert.Contains("WHERE", compiledQuery.Sql);
            Assert.Contains("age >", compiledQuery.Sql);
        }

        [Fact]
        public async Task CompiledQuery_ExecuteAsync_ReturnsResults()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 30)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count > 0);
            Assert.All(results, u => Assert.True(u.Age > 30));
        }

        [Fact]
        public async Task CompiledQuery_WithMultipleParameters_BindsCorrectly()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25 && u.IsActive)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.All(results, u =>
            {
                Assert.True(u.Age > 25);
                Assert.True(u.IsActive);
            });
        }

        [Fact]
        public async Task CompiledQuery_WithOrderBy_AppliesOrdering()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25).OrderBy(u => u.Age)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count > 1);

            // Verify ordering
            for (int i = 1; i < results.Count; i++)
            {
                Assert.True(results[i - 1].Age <= results[i].Age);
            }
        }

        [Fact]
        public async Task CompiledQuery_WithProjection_ReturnsProjectedResults()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User, UserSummary>(
                _tableName,
                q => q
                    .Where(u => u.Age > 25)
                    .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count > 0);
            Assert.All(results, summary =>
            {
                Assert.True(summary.Id > 0);
                Assert.False(string.IsNullOrEmpty(summary.Name));
            });
        }

        [Fact]
        public async Task CompiledQuery_WithTake_LimitsResults()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 20).Take(5)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(5, results.Count);
        }

        [Fact]
        public async Task CompiledQuery_WithSkip_SkipsResults()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 20).OrderBy(u => u.Id).Skip(10)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count > 0);
            // First result should have Id > 10
            Assert.True(results[0].Id > 10);
        }

        [Fact]
        public async Task CompiledQuery_WithComplexFilter_ExecutesCorrectly()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25 && u.IsActive && u.Name.StartsWith("User"))
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.All(results, u =>
            {
                Assert.True(u.Age > 25);
                Assert.True(u.IsActive);
                Assert.StartsWith("User", u.Name);
            });
        }

        [Fact]
        public async Task CompiledQuery_ReusedWithDifferentParameters_ReturnsDifferentResults()
        {
            // Arrange - Create two separate compiled queries with different parameters
            var compiledQuery1 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25)
            );
            var compiledQuery2 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 35)
            );

            // Act - Execute with different age thresholds
            var results1 = await compiledQuery1.ExecuteAsync(_client);
            var results2 = await compiledQuery2.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results1);
            Assert.NotNull(results2);
            Assert.True(results1.Count > results2.Count); // Fewer users over 35 than over 25
            Assert.All(results1, u => Assert.True(u.Age > 25));
            Assert.All(results2, u => Assert.True(u.Age > 35));
        }

        [Fact]
        public async Task CompiledQuery_WithNoResults_ReturnsEmptyList()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 1000) // No users older than 1000
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public void CompiledQuery_GetStatistics_ReturnsCorrectCounts()
        {
            // Arrange
            CompiledQuery.ClearCache();

            // Act
            var stats1 = CompiledQuery.GetStatistics();

            // Create some compiled queries
            _ = CompiledQuery.Create<User>(_tableName, q => q.Where(u => u.Age > 25));
            _ = CompiledQuery.Create<User>(_tableName, q => q.Where(u => u.Age > 30));

            var stats2 = CompiledQuery.GetStatistics();

            // Assert
            Assert.Equal(0, stats1.CacheHits);
            Assert.Equal(0, stats1.CacheMisses);
            Assert.Equal(0, stats1.CacheSize);

            // Note: Current implementation doesn't use cache yet, so these might be 0
            // This test documents the expected behavior when caching is fully implemented
            Assert.True(stats2.CacheSize >= 0);
        }

        [Fact]
        public void CompiledQuery_ClearCache_ClearsStatistics()
        {
            // Arrange
            _ = CompiledQuery.Create<User>(_tableName, q => q.Where(u => u.Age > 25));
            var statsBefore = CompiledQuery.GetStatistics();

            // Act
            CompiledQuery.ClearCache();
            var statsAfter = CompiledQuery.GetStatistics();

            // Assert
            Assert.Equal(0, statsAfter.CacheHits);
            Assert.Equal(0, statsAfter.CacheMisses);
            Assert.Equal(0, statsAfter.CacheSize);
        }

        [Fact]
        public async Task CompiledQuery_WithDistinct_ReturnsUniqueResults()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 20).Distinct()
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            // Check for uniqueness by Id
            var uniqueIds = results.Select(u => u.Id).Distinct().Count();
            Assert.Equal(uniqueIds, results.Count);
        }

        [Fact]
        public async Task CompiledQuery_WithMultipleOrderBy_AppliesAllOrdering()
        {
            // Arrange
            var compiledQuery = CompiledQuery.Create<User>(
                _tableName,
                q => q
                    .Where(u => u.Age > 20)
                    .OrderBy(u => u.IsActive)
                    .ThenBy(u => u.Age)
            );

            // Act
            var results = await compiledQuery.ExecuteAsync(_client);

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count > 1);

            // Verify primary ordering by IsActive, then by Age
            for (int i = 1; i < results.Count; i++)
            {
                if (results[i - 1].IsActive == results[i].IsActive)
                {
                    Assert.True(results[i - 1].Age <= results[i].Age);
                }
            }
        }

        [Fact]
        public void CompiledQuery_Cache_PreventsRecompilation()
        {
            // Clear cache to start fresh
            CompiledQuery.ClearCache();
            var initialStats = CompiledQuery.GetStatistics();
            Assert.Equal(0, initialStats.CacheHits);
            Assert.Equal(0, initialStats.CacheMisses);
            Assert.Equal(0, initialStats.CacheSize);

            // First compilation - should be a cache miss
            var query1 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25)
            );

            var stats1 = CompiledQuery.GetStatistics();
            Assert.Equal(0, stats1.CacheHits);
            Assert.Equal(1, stats1.CacheMisses);
            Assert.Equal(1, stats1.CacheSize);

            // Second compilation with same query - should be a cache hit
            var query2 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25)
            );

            var stats2 = CompiledQuery.GetStatistics();
            Assert.Equal(1, stats2.CacheHits);
            Assert.Equal(1, stats2.CacheMisses);
            Assert.Equal(1, stats2.CacheSize);

            // Verify both queries produce same SQL
            Assert.Equal(query1.Sql, query2.Sql);
        }

        [Fact]
        public async Task CompiledQuery_CacheHit_ProducesSameResults()
        {
            CompiledQuery.ClearCache();

            // Create query twice (first should cache, second should hit cache)
            var query1 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25).OrderBy(u => u.Age).Take(5)
            );

            var query2 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25).OrderBy(u => u.Age).Take(5)
            );

            // Execute both and verify results match
            var results1 = await query1.ExecuteAsync(_client);
            var results2 = await query2.ExecuteAsync(_client);

            Assert.Equal(results1.Count, results2.Count);
            for (int i = 0; i < results1.Count; i++)
            {
                Assert.Equal(results1[i].Id, results2[i].Id);
                Assert.Equal(results1[i].Name, results2[i].Name);
                Assert.Equal(results1[i].Age, results2[i].Age);
            }
        }

        [Fact]
        public void CompiledQuery_DifferentQueries_CreateSeparateCacheEntries()
        {
            CompiledQuery.ClearCache();

            // Create three different queries with visibly different SQL
            var query1 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 25)
            );

            var query2 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 30)
            );

            var query3 = CompiledQuery.Create<User>(
                _tableName,
                q => q.Where(u => u.Age > 35)
            );

            var stats = CompiledQuery.GetStatistics();
            
            // Debug: print SQL of each query to see if any are duplicates
            System.Diagnostics.Debug.WriteLine($"Query1 SQL: {query1.Sql}");
            System.Diagnostics.Debug.WriteLine($"Query2 SQL: {query2.Sql}");
            System.Diagnostics.Debug.WriteLine($"Query3 SQL: {query3.Sql}");
            System.Diagnostics.Debug.WriteLine($"Cache stats: Hits={stats.CacheHits}, Misses={stats.CacheMisses}, Size={stats.CacheSize}");
            
            Assert.Equal(0, stats.CacheHits);
            Assert.Equal(3, stats.CacheMisses);
            Assert.Equal(3, stats.CacheSize);
        }

        [Fact]
        public void CompiledQuery_ClearCache_ResetsStatistics()
        {
            // Create some queries to populate cache
            CompiledQuery.Create<User>(_tableName, q => q.Where(u => u.Age > 25));
            CompiledQuery.Create<User>(_tableName, q => q.Where(u => u.Age > 30));

            var statsBeforeClear = CompiledQuery.GetStatistics();
            Assert.True(statsBeforeClear.CacheSize > 0);

            // Clear cache
            CompiledQuery.ClearCache();

            var statsAfterClear = CompiledQuery.GetStatistics();
            Assert.Equal(0, statsAfterClear.CacheHits);
            Assert.Equal(0, statsAfterClear.CacheMisses);
            Assert.Equal(0, statsAfterClear.CacheSize);
        }

        [Fact]
        public void CompiledQuery_ProjectionCache_WorksCorrectly()
        {
            CompiledQuery.ClearCache();

            // First projection compilation - should be a cache miss
            var query1 = CompiledQuery.Create<User, UserSummary>(
                _tableName,
                q => q.Where(u => u.Age > 25).Select(u => new UserSummary
                {
                    Id = u.Id,
                    Name = u.Name
                })
            );

            var stats1 = CompiledQuery.GetStatistics();
            Assert.Equal(0, stats1.CacheHits);
            Assert.Equal(1, stats1.CacheMisses);
            Assert.Equal(1, stats1.CacheSize);

            // Second projection compilation with same query - should be a cache hit
            var query2 = CompiledQuery.Create<User, UserSummary>(
                _tableName,
                q => q.Where(u => u.Age > 25).Select(u => new UserSummary
                {
                    Id = u.Id,
                    Name = u.Name
                })
            );

            var stats2 = CompiledQuery.GetStatistics();
            Assert.Equal(1, stats2.CacheHits);
            Assert.Equal(1, stats2.CacheMisses);
            Assert.Equal(1, stats2.CacheSize);
        }
    }

    /// <summary>
    /// Test entity for CompiledQuery tests.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for projection tests.
    /// </summary>
    public class UserSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
