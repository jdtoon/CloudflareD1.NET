# Changelog

All notable changes to CloudflareD1.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

