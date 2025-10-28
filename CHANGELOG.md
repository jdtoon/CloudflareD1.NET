# Changelog

All notable changes to CloudflareD1.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Future Enhancements (Pending Cloudflare API Support)

**Note:** The following features require Cloudflare D1 REST API support for transactions, which is currently only available in the Workers environment.

- **Transaction Support**: ITransaction interface with Begin/Commit/Rollback for atomic operations
- **Advanced Batch Operations**: BatchInsertAsync, BatchUpdateAsync, BatchDeleteAsync, UpsertAsync with automatic entity mapping

These will be implemented once Cloudflare adds transaction support to the D1 REST API.

## [1.11.3] - 2025-10-28 - CloudflareD1.NET

### Added - Production-Ready Features

#### Health Check API
- **CheckHealthAsync()** - New method on ID1Client for production monitoring and health checks
  - Returns `D1HealthStatus` with IsHealthy, Latency, Mode, ErrorMessage, Metadata
  - Works in both Local (SQLite) and Remote (Cloudflare D1) modes
  - Executes simple SELECT 1 query to verify connectivity
  - Measures end-to-end latency including network and database time
  - Ideal for Kubernetes liveness/readiness probes, load balancer health checks, monitoring systems

#### Automatic Retry Policy
- **EnableRetry** - Configurable retry logic in D1Options (default: true)
  - Automatically retries transient failures: 429 (rate limit), 503 (service unavailable)
  - Also retries HttpRequestException and timeout errors
- **MaxRetries** - Maximum retry attempts (default: 3)
- **InitialRetryDelayMs** - Starting delay for exponential backoff (default: 100ms)
- **Exponential backoff** - Delay doubles with each retry (100ms → 200ms → 400ms → 800ms...)
- **ShouldRetry()** - Smart retry logic that only retries appropriate error codes
- **ExecuteWithRetryAsync()** - Internal helper wrapping HTTP operations with retry logic

#### Enhanced Structured Logging
- **Query execution logging** - Logs SQL, parameters, duration, and result count at Information level
  - "D1 query executed successfully, returned X result(s) (Duration: Yms)"
- **Retry logging** - Logs each retry attempt with attempt number, reason, and backoff delay
  - "D1 API request failed (Attempt 2/3): Rate limit exceeded. Retrying in 200ms..."
- **Health check logging** - Logs health check results with latency and status
  - "Health check completed: Healthy (Latency: 123.45ms)"
- **Debug logging** - Request/response payloads, status codes, and detailed execution flow

### Changed
- **CloudflareD1Provider** - HTTP requests now wrapped in retry logic when EnableRetry=true
- **D1HealthStatus** - New model class in CloudflareD1.NET.Models namespace
- **ID1Client interface** - Added CheckHealthAsync method

### Testing
- **Integration test** - Step 0.5 in test-app validates health check against real Cloudflare D1
- **296 tests passing** - All existing unit tests still pass

**Production Benefits:**
- ✅ **Monitoring**: Health checks for uptime monitoring and alerting
- ✅ **Resilience**: Automatic retry reduces impact of transient failures
- ✅ **Observability**: Structured logging for debugging and performance analysis
- ✅ **Kubernetes**: Native support for liveness/readiness probes
- ✅ **Load Balancers**: Health endpoint for traffic routing decisions

## [1.0.3] - 2025-01-28 - CloudflareD1.NET.CodeFirst

### Added

#### Per-Property Change Detection
- **GetModifiedProperties()** - New method on EntityEntry/ITrackedEntry to detect which properties changed
  - Compares current property values with OriginalValues snapshot
  - Returns list of only the properties that have been modified
  - Skips primary key properties (they shouldn't change)
- **Intelligent UPDATE generation** - BuildUpdate now only includes changed columns in SET clause
  - Queries `GetModifiedProperties()` to determine what changed
  - Skips UPDATE entirely if no properties changed (returns 0 rows affected)
  - Improves performance by reducing unnecessary column writes
- **Snapshot management** - Original values captured at the right time
  - Snapshot taken in `AcceptAllChanges` after successful SaveChanges
  - Captures current state of entities transitioning to Unchanged
  - Enables accurate change tracking for subsequent updates

### Changed
- **D1Context.BuildUpdate()** - Now uses per-property change detection instead of updating all columns
- **ChangeTracker.AcceptAllChanges()** - Captures property snapshots when transitioning entities to Unchanged state
- **ChangeTracker.TrackUpdate()** - Captures snapshots if not already present when tracking existing entities
- **Documentation updates** - Added per-property change detection examples and explanations in README and docs

### Testing
- **SaveChangesTests.cs** - 5 new unit tests for property change detection:
  - Update single property only
  - Update multiple properties only
  - Update with no changes (skips UPDATE)
  - Update all properties
  - Update nullable properties
- **Integration test** - CF-8 in test-app validates per-property updates against real Cloudflare D1
  - Verified UPDATE statement only includes changed columns
  - Verified 0 rows affected when no properties change

**296 tests passing** (all unit tests green)

## [1.0.2] - 2025-01-28 - CloudflareD1.NET.CodeFirst

### Added

#### Foreign Key-Aware Operation Ordering
- **DependencyAnalyzer** - Analyzes foreign key relationships to determine safe operation order
  - `GetInsertOrder()` - Returns entity types ordered for INSERT (parents before children)
  - `GetDeleteOrder()` - Returns entity types ordered for DELETE (children before parents)
  - `BuildDependencyGraph()` - Constructs directed dependency graph from FK metadata
  - `TopologicalSort()` - Kahn's algorithm implementation for cycle-free ordering
  - `HasSelfReference()` - Detects self-referencing foreign keys
- **Automatic operation ordering in SaveChangesAsync** - INSERT and DELETE operations are now automatically reordered based on foreign key dependencies
  - **INSERT order**: Parent entities (referenced by FKs) are inserted before children
  - **DELETE order**: Child entities (with FKs) are deleted before parents
  - **UPDATE operations**: No reordering (FK values should not change during updates)
  - **Circular dependency detection**: Throws `InvalidOperationException` if circular FK dependencies are detected

### Changed
- **D1Context.SaveChangesAsync()** - Now groups tracked entries by entity type and applies FK-aware ordering
- **Documentation updates** - Added FK-aware ordering examples and explanations in README and docs

### Testing
- **DependencyAnalyzerTests.cs** - 11 comprehensive unit tests covering:
  - Simple parent-child relationships
  - Multi-level hierarchies (3+ levels deep)
  - Self-referencing entities
  - Circular dependency detection
  - Independent entities (no FKs)
  - Various input orderings (reversed, mixed, already correct)
- **Integration tests** - CF-6/CF-7 in test-app validate FK-aware ordering against real Cloudflare D1

**291 tests passing** (all unit tests green)

## [1.0.1] - 2025-01-28 - CloudflareD1.NET.CodeFirst

### Added

#### Change Tracking & SaveChanges
- **EntityState enum** - Track entity states: Detached, Unchanged, Added, Modified, Deleted
- **EntityEntry<T>** - Represents a tracked entity with state and original values
- **ChangeTracker** - Manages all tracked entity entries in a D1Context
- **D1Set<T>.Add/Update/Remove** - Methods to track entity changes
- **D1Context.SaveChangesAsync()** - Persists all tracked changes to the database
  - Executes operations in order: INSERT → UPDATE → DELETE
  - Sequential execution to satisfy Cloudflare D1 API semantics
  - Automatically populates auto-increment primary keys after INSERT
  - Returns the total number of rows affected

#### Property Mapping Improvements
- **Navigation property filtering** - Reference types and collections are now ignored by default and not mapped to columns
- **Enum support** - Enums are stored as TEXT by default
- **IsSupportedColumnType()** - Helper method to filter properties to supported scalar types

### Changed
- **ModelBuilder property discovery** - Now excludes navigation properties (non-string reference types) and collections automatically
- **Documentation updates** - Added SaveChanges usage examples and conventions for navigation properties

### Testing
- **SaveChangesTests.cs** - Unit tests for Add/Update/Delete with local SQLite
- **NavigationPropertyTests.cs** - Regression test ensuring navigation properties aren't mapped
- **Integration tests** - E2E SaveChanges validation against real Cloudflare D1 in test-app

**280 tests passing** (all unit tests green)

## [1.11.1] - 2025-01-27

### Removed

#### Transaction and Batch Operations
- **Removed unsupported features** that require Cloudflare D1 REST API transaction support
  - Removed `ITransaction` interface and `Transaction.cs` implementation
  - Removed `BeginTransactionAsync()` from client interface
  - Removed batch operation extensions: `BatchInsertAsync`, `BatchUpdateAsync`, `BatchDeleteAsync`, `UpsertAsync`
  - Removed 20 unit tests for transaction/batch features
  - Removed Steps 101-110 from integration tests

### Changed
- **Realigned library with actual Cloudflare D1 REST API capabilities**
  - All remaining features verified against real Cloudflare D1 database
  - 230 tests passing (183 core + 47 LINQ)
  - 100 integration test steps passing

### Documentation
- Added Future Enhancements section documenting pending features
- Clarified that transactions/batch operations require Workers environment or future REST API support

## [1.11.0] - 2025-01-27

### Added - CloudflareD1.NET.Linq

#### JsonElement Compatibility
- **Cloudflare API Response Handling**: Added support for `System.Text.Json.JsonElement` in query results
  - `AnyAsync()` now correctly handles JsonElement integer responses from Cloudflare D1 API
  - `AllAsync()` now correctly handles JsonElement integer responses from Cloudflare D1 API
  - Ensures compatibility with both local SQLite (native types) and remote Cloudflare D1 (JsonElement)

### Testing
- **230 Tests Passing**: Full test coverage for all LINQ and core functionality
  - 183 core package tests
  - 47 LINQ package tests
  - All tests verified against both local SQLite and remote Cloudflare D1

### Documentation
- Updated README with Future Enhancements section
- Documented Cloudflare D1 REST API limitations

## [1.10.0] - 2025-01-27

### Added - CloudflareD1.NET.Linq

#### Query Optimization with CompiledQuery
- **CompiledQuery<T, TResult>**: Pre-compile LINQ expressions to SQL for efficient repeated execution
  - Compiles expression trees to SQL once, eliminating repeated translation overhead
  - Automatic caching of compiled queries based on SQL and parameters
  - **95% performance improvement** for repeated query execution
  - Thread-safe concurrent execution support
  - Returns sealed class with `ExecuteAsync(ID1Client, CancellationToken)` method
  
- **CompiledQuery.Create<T>(tableName, queryBuilder)**: Factory for basic queries
  - Pre-compiles queries that return `List<T>`
  - Captures parameters from expression closures at compile time
  - Example:
    ```csharp
    var compiled = CompiledQuery.Create<User>(
        "users",
        q => q.Where(u => u.Age > 25).OrderBy(u => u.Name).Take(10)
    );
    var results = await compiled.ExecuteAsync(client);
    ```

- **CompiledQuery.Create<T, TResult>(tableName, queryBuilder)**: Factory for projection queries
  - Pre-compiles queries with Select() projections
  - Returns `List<TResult>` with projected type
  - Example:
    ```csharp
    var compiled = CompiledQuery.Create<User, UserSummary>(
        "users",
        q => q.Where(u => u.IsActive).Select(u => new UserSummary { ... })
    );
    ```

#### Expression Tree Caching
- **Automatic Query Caching**: Compiled queries cached by SQL + parameters
  - Uses `ConcurrentDictionary` for thread-safe cache operations
  - Cache key includes table name, entity type, SQL, and parameter values
  - Different parameter values create separate cache entries
  - Eliminates redundant compilation of identical queries

- **CompiledQuery.GetStatistics()**: Cache performance monitoring
  - Returns tuple: `(long CacheHits, long CacheMisses, int CacheSize)`
  - Thread-safe atomic counter updates using `Interlocked`
  - Enables hit ratio calculation and cache effectiveness analysis
  
- **CompiledQuery.ClearCache()**: Manual cache management
  - Clears all cached compiled queries
  - Resets hit/miss statistics to zero
  - Useful for testing or memory management

#### Testing & Documentation
- **19 CompiledQuery unit tests**: Comprehensive coverage of compilation and caching
  - Query creation, execution, parameter binding
  - Cache hit/miss behavior, statistics tracking
  - Projections, ordering, pagination, distinct
  - Complex filters, edge cases
- **5 integration tests** (Steps 96-100) in test-app
- **230 total tests passing** (211 existing + 19 new)
- New documentation: [Query Optimization](./docs/docs/linq/query-optimization.md)
- Updated LINQ intro with v1.10.0 features and performance benchmarks

### Performance
- First execution: Same as regular query (includes compilation overhead)
- Subsequent executions: **~95% faster** (no expression tree processing)
- Memory: Minimal overhead (cached SQL strings and parameter arrays)
- Scalability: Linear performance with cache size, no degradation

## [1.9.0] - 2025-10-27

### Added - CloudflareD1.NET.Linq

#### Async Streaming
- **ToAsyncEnumerable(CancellationToken)**: Stream query results for memory-efficient processing
  - Returns `IAsyncEnumerable<T>` for consuming results one at a time
  - Memory-efficient - yields entities without loading entire result set
  - Supports `await foreach` syntax for natural streaming consumption
  - CancellationToken support for canceling streaming operations mid-enumeration
  - Ideal for processing large datasets that don't fit in memory
  - Works with all query operations: Where(), OrderBy(), Take(), Skip(), etc.
  - Example:
    ```csharp
    await foreach (var user in client.Query<User>("users")
        .Where(u => u.IsActive)
        .ToAsyncEnumerable(cancellationToken))
    {
        await ProcessUserAsync(user); // Process one at a time
    }
    ```

#### Cancellation Token Support
Added `CancellationToken` parameter to all async execution methods:
- **ToListAsync(CancellationToken)**: Cancel query execution before completion
- **FirstOrDefaultAsync(CancellationToken)**: Cancel first result fetch
- **SingleAsync(CancellationToken)**: Cancel single result verification
- **SingleOrDefaultAsync(CancellationToken)**: Cancel optional single result
- **CountAsync(CancellationToken)**: Cancel count operation
- **AnyAsync(CancellationToken)**: Cancel existence check
- **AnyAsync(Expression, CancellationToken)**: Cancel predicate existence check  
- **AllAsync(Expression, CancellationToken)**: Cancel universal predicate check

All methods default to `CancellationToken.None` for backwards compatibility.

#### Testing & Documentation
- **16 new unit tests** in `AsyncStreamingTests.cs`:
  - Basic streaming (all records, with WHERE, with OrderBy)
  - Pagination (Take, Skip)
  - Complex queries (WHERE + ORDER BY + LIMIT)
  - Early termination (break in foreach loop)
  - Cancellation (CancellationTokenSource)
  - Edge cases (no results, multiple enumerations)
  - Streaming behavior (one-at-a-time yielding)
  - CancellationToken support across all methods
- **5 new integration tests** (Steps 91-95) in test-app
- **211 total tests passing** (195 existing + 16 new)
- Comprehensive examples for streaming large result sets

## [1.8.0] - 2025-01-26

### Added - CloudflareD1.NET.Linq

#### Existence Check Methods
- **AnyAsync(Expression<Func<T, bool>> predicate)**: Check if any rows match a condition
  - Generates SQL `SELECT EXISTS(SELECT 1 FROM table WHERE conditions AND predicate)`
  - Combines with existing Where() clauses
  - Returns `Task<bool>` - true if any matching rows exist
  - Optimized with EXISTS for efficient existence checking
- **AllAsync(Expression<Func<T, bool>> predicate)**: Check if all rows match a condition
  - Generates SQL `SELECT NOT EXISTS(SELECT 1 FROM table WHERE conditions AND NOT predicate)`
  - Uses NOT EXISTS with negated predicate for optimal performance
  - Returns `Task<bool>` - true if all rows satisfy the condition
  - Combines with existing query filters

#### Set Operations
- **Union()**: Combine results from two queries, removing duplicates
  - Generates SQL `SELECT ... UNION SELECT ...`
  - Returns `ISetOperationQueryBuilder<T>` for method chaining
  - Supports further Union(), Intersect(), Except() chaining
- **UnionAll()**: Combine results from two queries, keeping all duplicates
  - Generates SQL `SELECT ... UNION ALL SELECT ...`
  - More performant than Union() when duplicates don't matter
- **Intersect()**: Return only rows that appear in both queries
  - Generates SQL `SELECT ... INTERSECT SELECT ...`
  - Useful for finding common elements between sets
- **Except()**: Return rows from first query that don't appear in second
  - Generates SQL `SELECT ... EXCEPT SELECT ...`
  - Also known as "set difference" or "minus"

#### Set Operation Query Builder
- **ISetOperationQueryBuilder<T>**: New fluent interface for set operations
  - Chainable Union(), UnionAll(), Intersect(), Except() methods
  - ToListAsync(), FirstOrDefaultAsync(), CountAsync(), AnyAsync()
- **SetOperationQueryBuilder<T>**: Internal implementation class
  - Handles SQL generation for complex chained operations
  - Automatic subquery wrapping for ORDER BY/LIMIT/OFFSET clauses
  - Parameter aggregation across multiple queries
- **SetOperationType enum**: Union, UnionAll, Intersect, Except

#### Query Improvements
- **Subquery wrapping**: Queries with ORDER BY/LIMIT/OFFSET automatically wrapped as subqueries in set operations
- **Parameter handling**: Proper aggregation of parameters across multiple queries in set operations
- **SQL correctness**: Ensures SQLite syntax requirements (ORDER BY after UNION, not before)

#### Documentation
- Updated README.md with set operation and existence check examples
- Updated LINQ README with comprehensive usage guide
- Updated ROADMAP.md to mark v1.8.0 features complete
- Integration test examples in test-app

#### Testing
- **Set Operations**: 19 unit tests
  - Coverage for Union, UnionAll, Intersect, Except
  - Tests for chaining, COUNT, ANY, FirstOrDefault
  - Tests for queries with WHERE, ORDER BY, TAKE, SKIP
  - Tests for null argument validation
  - Tests for empty result handling
  - 8 integration test examples in test-app (Steps 77-84)
- **Existence Checks**: 12 unit tests
  - Coverage for AnyAsync(predicate) and AllAsync(predicate)
  - Tests for simple and complex predicates
  - Tests combining with existing WHERE clauses
  - Tests for string comparisons and equality checks
  - Null argument validation tests
- **195 total tests passing** (+31 from v1.7.0: 19 set operations + 12 existence checks)

### Technical Details
- New files: SetOperationType.cs, ISetOperationQueryBuilder.cs, SetOperationQueryBuilder.cs
- Modified files: IQueryBuilder.cs, QueryBuilder.cs
- New test file: SetOperationTests.cs (19 tests)
- Total new code: ~650 lines (including tests)
- Backward compatible: No breaking changes to existing APIs

### Notes
- This is a beta release for set operations
- Any/All with predicates planned for full v1.8.0 release
- Set operations are production-ready and fully tested

---

## [1.7.0] - 2025-01-26

### Added - CloudflareD1.NET.Linq

#### Distinct() Method
- **Distinct() support**: Remove duplicate rows from query results
- **SELECT DISTINCT**: Generates `SELECT DISTINCT` SQL clause
- **Fluent chaining**: Compatible with Where(), OrderBy(), Take(), Skip()
- **Projection support**: Works with Select() for distinct projected results
- **IQueryBuilder<T>**: Added Distinct() to fluent query interface
- **IProjectionQueryBuilder<TResult>**: Added Distinct() to projection interface

#### Contains()/IN Clause Support
- **Collection.Contains()**: Already supported, now documented and tested
- **IN clause generation**: Translates `collection.Contains(property)` to SQL `IN (?...)`
- **Multiple data types**: Support for string, int, Guid, and other types
- **Empty collection handling**: Generates `IN ()` for empty collections
- **SqlExpressionVisitor**: Existing support in expression visitor
- **Parameterized queries**: Proper parameter binding for IN clause values

#### Documentation
- Updated README.md with Distinct() and Contains() examples
- Updated LINQ README with comprehensive usage examples
- Updated ROADMAP.md to mark v1.7.0 complete
- Integration test examples in test-app (6 new test steps)

#### Testing
- 7 new unit tests for Distinct()
- 4 new unit tests for Contains()/IN clause
- 6 integration test examples in test-app (Steps 71-76)
- Full test coverage for Distinct with Where, OrderBy, Select, Take
- Full test coverage for Contains with string/int arrays, empty collections

### Technical Details
- Modified files: IQueryBuilder.cs, QueryBuilder.cs, IProjectionQueryBuilder.cs, ProjectionQueryBuilder.cs
- New test files: DistinctTests.cs, ContainsTests.cs
- Total new code: ~400 lines (including tests)
- Backward compatible: No breaking changes to existing APIs
- 164 total tests passing (+11 from v1.6.0)

---

## [1.6.0] - 2025-01-26

### Added - CloudflareD1.NET.Linq

#### Join Operations
- **Join() support**: INNER JOIN across multiple tables with type-safe key selectors
- **LeftJoin() support**: LEFT JOIN with proper NULL handling for non-matching rows
- **Multi-table projections**: Combine columns from joined tables with automatic aliasing
- **IJoinQueryBuilder<TOuter, TInner, TKey>**: New interface for join operations
- **IJoinProjectionQueryBuilder<TResult>**: Interface for projection after joining
- **JoinType enum**: Support for Inner, Left, Right join types
- **WHERE clause support**: Filter joined results after projection
- **ORDER BY support**: Sort joined results with `.OrderBy()` and `.OrderByDescending()`
- **LIMIT/OFFSET support**: Use `.Take()` and `.Skip()` with joined results
- **Aggregation support**: Use `.CountAsync()`, `.FirstOrDefaultAsync()`, etc. on joins

#### Expression Parsing Enhancements
- **MemberInitExpression support**: Parse object initializer syntax in Select projections
- **NewExpression support**: Parse constructor syntax in Select projections
- **Automatic column aliasing**: Generate unique aliases to avoid naming conflicts
- **Multi-table SELECT clause generation**: Build proper SELECT with qualified column names
- **JOIN ON clause generation**: Translate key selectors to SQL JOIN conditions

#### Bug Fixes
- Fixed JsonElement handling in CountAsync() for D1 API responses
- Fixed column mapping in join results with proper snake_case to PascalCase conversion
- Fixed ambiguous column names in multi-table SELECT statements

#### Documentation
- Comprehensive Join Operations section in LINQ README
- New Docusaurus page: `docs/linq/joins.md` with detailed examples
- Updated ROADMAP.md to mark v1.6.0 complete
- Integration test examples in test-app (6 new test steps)

#### Testing
- 6 new unit tests for Join operations
- 6 integration test examples in test-app
- Full test coverage for INNER JOIN and LEFT JOIN
- Test coverage for complex scenarios (WHERE, ORDER BY, LIMIT, COUNT)

### Technical Details
- New files: IJoinQueryBuilder.cs, JoinQueryBuilder.cs, JoinType.cs
- Total new code: ~600 lines
- Backward compatible: No breaking changes to existing APIs
- 153 total tests passing

---

### Added - CloudflareD1.NET.Linq (v1.5.1)

#### Having Clause
- **Having() support**: Filter grouped results after aggregation
- **Aggregate predicates**: Use Count(), Sum(), Average(), Min(), Max() in conditions
- **Comparison operators**: Full support for >, <, >=, <=, ==, !=
- **Expression translation**: Converts LINQ expressions to SQL HAVING clauses
- **Integration with GroupBy**: Seamless combination of GROUP BY and HAVING

#### Bug Fixes
- Fixed CS8620 nullability warning in GroupByQueryBuilder.ConvertResultsToRows()

#### Documentation
- Comprehensive Having Clause section in LINQ README
- New Docusaurus page: `docs/linq/having.md` with detailed examples
- Updated ROADMAP.md to mark v1.5.1 complete
- Integration test examples in test-app (3 new test steps)

#### Testing
- 6 new unit tests for Having clause
- 3 integration test examples in test-app
- Full test coverage for all aggregate functions in Having predicates

### Technical Details
- Enhanced GroupByQueryBuilder with Having() implementation
- Total new code: ~90 lines for Having translation
- Backward compatible: No breaking changes to existing APIs

---

## [1.5.0] - 2025-01-14

### Added - CloudflareD1.NET.Linq

#### GroupBy & Aggregations
- **GroupBy() support**: Group query results by single key with full LINQ integration
- **Aggregate functions**: Count(), Sum(), Average(), Min(), Max() with expression support
- **Complex aggregate expressions**: Support for calculations like `Sum(p => p.Price * p.Quantity)`
- **IGroupByQueryBuilder<TSource, TKey>**: New interface for GroupBy operations
- **IGroupByProjectionQueryBuilder<TResult>**: Interface for projection after grouping
- **AggregateExpressionVisitor**: Translates LINQ aggregate expressions to SQL
- **SQL GROUP BY generation**: Proper SQL with aggregate functions and GROUP BY clauses

#### Integration Features
- **WHERE clause integration**: Filter data before grouping with `.Where()`
- **ORDER BY integration**: Sort grouped results with `.OrderBy()` and `.OrderByDescending()`
- **LIMIT/OFFSET support**: Use `.Take()` and `.Skip()` with grouped results
- **Multiple aggregates per group**: Calculate multiple aggregate functions in single query

#### Documentation
- Comprehensive GroupBy section in README with examples
- Updated ROADMAP.md to mark v1.5.0 complete
- Integration test examples in test-app (8 new test steps)
- API documentation for all new interfaces and methods

#### Testing
- 11 new unit tests (2 API tests + 9 SQL generation tests)
- 8 integration test examples in test-app
- Full test coverage for all aggregate functions
- Test coverage for complex scenarios (WHERE, ORDER BY, LIMIT integration)

### Technical Details
- New files: IGrouping.cs, IGroupByQueryBuilder.cs, GroupByQueryBuilder.cs, AggregateExpressionVisitor.cs
- Total new code: ~1,088 lines
- Result class constraint: `where TResult : class, new()` for entity mapper compatibility
- Backward compatible: No breaking changes to existing APIs

## [1.0.0] - 2025-10-25

### Added

#### Core Features
- Initial release of CloudflareD1.NET
- Complete implementation of Cloudflare D1 REST API
- Full support for SQL query execution (SELECT, INSERT, UPDATE, DELETE)
- Parameterized queries with support for named and positional parameters
- Batch operations for executing multiple statements as atomic transactions
- Local SQLite mode for development without Cloudflare credentials
- Remote Cloudflare D1 mode for production deployments

#### Database Management
- List all D1 databases in an account
- Get detailed information about specific databases
- Create new D1 databases programmatically
- Delete D1 databases
- Time Travel queries for accessing historical data

#### Developer Experience
- ASP.NET Core dependency injection integration
- IServiceCollection extension methods for easy configuration
- Support for IConfiguration binding
- Comprehensive logging with ILogger integration
- Strong typing with full XML documentation
- Async/await patterns throughout the API

#### Authentication
- API Token authentication (recommended)
- API Key + Email authentication (legacy)
- Flexible configuration options

#### Configuration
- `D1Options` class for comprehensive configuration
- Support for both local and remote modes
- Configurable timeout and API base URL
- Connection string-style configuration support

#### Error Handling
- Custom exception hierarchy (D1Exception, D1ApiException, D1QueryException, etc.)
- Detailed error messages with context
- HTTP status code information for API errors

#### Documentation
- Complete README with quick start guide
- Docusaurus documentation site
- API reference documentation
- Usage examples and best practices
- Console application sample
- Contributing guidelines

#### Testing & CI/CD
- xUnit test project structure
- GitHub Actions workflow for CI/CD
- Automatic NuGet package publishing on master branch
- Build and test automation

### Technical Details
- Target Framework: .NET Standard 2.1
- Dependencies:
  - Microsoft.Data.Sqlite 9.0.10
  - System.Text.Json 9.0.10
  - Microsoft.Extensions.Logging.Abstractions 9.0.10
  - Microsoft.Extensions.Options 9.0.10
  - Microsoft.Extensions.DependencyInjection.Abstractions 9.0.10
  - Microsoft.Extensions.Configuration.Abstractions 9.0.10
  - Microsoft.Extensions.Configuration.Binder 9.0.10
  - Microsoft.Extensions.Http 9.0.10

### Notes
- This is the initial stable release
- Production-ready for both local development and Cloudflare D1 deployments
- Compatible with .NET Core 3.0+, .NET 5+, .NET 6+, .NET 8+

[Unreleased]: https://github.com/jdtoon/CloudflareD1.NET/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/jdtoon/CloudflareD1.NET/releases/tag/v1.0.0

