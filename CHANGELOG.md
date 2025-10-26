# Changelog

All notable changes to CloudflareD1.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.5.0] - 2024-12-14

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

