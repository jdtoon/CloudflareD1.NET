# CloudflareD1.NET Roadmap

## 📊 Current Status (October 2025)

### ✅ Released Versions

#### Core Package (CloudflareD1.NET)
- **v1.0.2** - Stable core library with D1 REST API support
  - Dual-mode (Local SQLite / Remote D1)
  - Batch operations & transactions
  - Time travel queries
  - Database management

#### LINQ Package (CloudflareD1.NET.Linq)
- **v1.8.0** - Set Operations & Existence Checks ✅ **CURRENT**
  - Union(), UnionAll(), Intersect(), Except() for set operations
  - AnyAsync(predicate), AllAsync(predicate) for existence checks
  - ISetOperationQueryBuilder<T> fluent interface
  - EXISTS/NOT EXISTS SQL patterns
  - Chainable set operations
  - Full WHERE/ORDER BY/LIMIT support

---

## 🎯 LINQ Roadmap - Path to v2.0

### Phase 1: Advanced Query Operations (v1.5.0 - v1.7.0)

#### v1.5.0 - Aggregation & GroupBy ✅ **COMPLETE**
**Released**: January 2025

**Features**:
- ✅ Basic aggregations: `Sum()`, `Average()`, `Min()`, `Max()`, `Count()`
- ✅ `GroupBy()` with single key
- ✅ Group aggregations: `group.Sum(x => x.Amount)`
- ✅ Complex expressions in aggregates: `g.Sum(p => p.Price * p.Quantity)`
- ✅ Multiple aggregations per group
- ✅ Integration with WHERE, ORDER BY, LIMIT

**Example**:
```csharp
var salesByCategory = await client.Query<Product>("products")
    .GroupBy(p => p.Category)
    .Select(g => new CategoryStats
    {
        Category = g.Key,
        TotalSales = g.Sum(p => p.Price * p.Quantity),
        AveragePrice = g.Average(p => p.Price),
        ProductCount = g.Count()
    })
    .ToListAsync();
```

**Completed**: January 2025
- GroupByQueryBuilder implementation
- AggregateExpressionVisitor for SQL translation
- SQL GROUP BY generation with aggregate functions
- 11 unit tests + 8 integration tests
- Full documentation and examples

---

#### v1.5.1 - Having Clause ✅ **COMPLETE**
**Released**: January 2025

**Features**:
- ✅ `Having()` clause for filtering grouped results
- ✅ Aggregate predicates in Having: `g.Count() > 10`, `g.Sum(x => x.Price) > 1000`
- ✅ Full comparison operator support: >, <, >=, <=, ==, !=
- ✅ Combination with WHERE, ORDER BY, LIMIT

**Example**:
```csharp
var largeGroups = await client.Query<User>("users")
    .Where(u => u.IsActive)
    .GroupBy(u => u.Country)
    .Having(g => g.Count() >= 10)
    .Select(g => new { Country = g.Key, Count = g.Count() })
    .OrderByDescending("count")
    .ToListAsync();
```

**Completed**: January 2025
- Having() predicate translation to SQL
- Expression visitor for aggregate predicates
- 6 unit tests + 3 integration tests
- Documentation and examples

---

#### v1.6.0 - Join Support ✅ **COMPLETE**
**Released**: January 2025

**Features**:
- ✅ `Join()` - Inner join with two tables
- ✅ `LeftJoin()` - Left outer join
- ✅ Multi-table projections with proper column aliasing
- ✅ Type-safe key selectors
- ✅ WHERE, ORDER BY, LIMIT, COUNT on joined results
- ✅ MemberInitExpression support (object initializer syntax)

**Example**:
```csharp
var ordersWithCustomers = await client.Query<Order>("orders")
    .Join(
        client.Query<Customer>("customers"),
        order => order.CustomerId,
        customer => customer.Id)
    .Select((order, customer) => new OrderWithCustomer
    {
        OrderId = order.Id,
        CustomerName = customer.Name,
        Total = order.Total
    })
    .Where(result => result.Total > 100)
    .ToListAsync();
```

**Completed**: January 2025
- JoinQueryBuilder implementation
- IJoinQueryBuilder and IJoinProjectionQueryBuilder interfaces
- SQL JOIN generation (INNER, LEFT)
- Multi-table SELECT clause parsing
- Proper column aliasing to avoid conflicts
- 6 unit tests + 6 integration tests
- Full documentation and examples

---

#### v1.7.0 - Advanced LINQ Methods
**Status**: ✅ **COMPLETE** (January 2025)

**Features**:
- ✅ `Distinct()` - Remove duplicate rows from results
- ✅ `Contains()` - IN clause support for collection filtering

**Example**:
```csharp
// Distinct
var uniqueCategories = await client.Query<Product>("products")
    .Select(p => new Product { Category = p.Category })
    .Distinct()
    .ToListAsync();

// Contains (IN clause)
var categories = new[] { "Electronics", "Books" };
var products = await client.AsQueryable<Product>("products")
    .Where(p => categories.Contains(p.Category))
    .ToListAsync();
```

**Completed**: January 2025
- Distinct() implementation in QueryBuilder and ProjectionQueryBuilder
- Contains() testing and documentation (already supported)
- SELECT DISTINCT SQL generation
- IN clause with proper parameterization
- 7 unit tests for Distinct()
- 4 unit tests for Contains()
- 6 integration tests in test-app
- Full documentation and examples

---

#### v1.8.0-beta - Set Operations ✅ **COMPLETE**
**Released**: January 2025

**Features**:
- ✅ `Union()` - Combine results from two queries, removing duplicates
- ✅ `UnionAll()` - Combine results keeping duplicates (more performant)
- ✅ `Intersect()` - Return only rows appearing in both queries
- ✅ `Except()` - Return rows from first query not in second (set difference)
- ✅ Chainable set operations: `.Union(q1).Union(q2).Intersect(q3)`
- ✅ `ISetOperationQueryBuilder<T>` fluent interface
- ✅ ToListAsync(), CountAsync(), AnyAsync(), FirstOrDefaultAsync() on set results
- ✅ Automatic subquery wrapping for ORDER BY/LIMIT/OFFSET
- ✅ Parameter aggregation across multiple queries

**Example**:
```csharp
// Union - combine young and senior users
var youngUsers = client.Query<User>("users").Where("age < ?", 30);
var seniorUsers = client.Query<User>("users").Where("age >= ?", 60);
var result = await youngUsers.Union(seniorUsers).ToListAsync();

// Intersect - users in both queries
var activeUsers = client.Query<User>("users").Where("is_active = ?", 1);
var premiumUsers = client.Query<User>("users").Where("is_premium = ?", 1);
var activePremium = await activeUsers.Intersect(premiumUsers).ToListAsync();

// Except - set difference
var allUsers = client.Query<User>("users");
var inactiveUsers = client.Query<User>("users").Where("is_active = ?", 0);
var activeOnly = await allUsers.Except(inactiveUsers).ToListAsync();

// Chained operations
var young = client.Query<User>("users").Where("age < ?", 25);
var middle = client.Query<User>("users").Where("age = ?", 40);
var senior = client.Query<User>("users").Where("age > ?", 60);
var nonMiddleAge = await young.Union(senior).ToListAsync();
```

**Completed**: January 2025
- SetOperationType enum (Union, UnionAll, Intersect, Except)
- ISetOperationQueryBuilder<T> interface
- SetOperationQueryBuilder<T> implementation
- Union(), UnionAll(), Intersect(), Except() methods in QueryBuilder
- Automatic SQL generation with proper syntax (ORDER BY after UNION)
- Subquery wrapping for queries with ORDER BY/LIMIT/OFFSET
- 19 unit tests for set operations
- 8 integration tests in test-app
- Full documentation and examples
- 183 total tests passing

---

#### v1.8.0 - Set Operations & Existence Checks ✅ **COMPLETE**
**Released**: January 2025

Completes the v1.8.0 release by adding existence check methods with predicates.

**New Features**:
- ✅ `AnyAsync(Expression<Func<T, bool>> predicate)` - Check if any rows match a condition
- ✅ `AllAsync(Expression<Func<T, bool>> predicate)` - Check if all rows match a condition
- ✅ EXISTS/NOT EXISTS SQL patterns for optimal performance
- ✅ Combines with existing Where() clauses
- ✅ Full expression tree translation support

**Example**:
```csharp
// Check if any users are over 35
var hasOldUsers = await client.Query<User>("users")
    .AnyAsync(u => u.Age > 35);

// Check if all users are over 18
var allAdults = await client.Query<User>("users")
    .AllAsync(u => u.Age > 18);

// Combine with existing filters
var hasYoungAlice = await client.Query<User>("users")
    .Where(u => u.Name == "Alice")
    .AnyAsync(u => u.Age < 30);

// Complex predicates
var allActiveOrYoung = await client.Query<User>("users")
    .AllAsync(u => u.IsActive || u.Age < 25);
```

**Completed**: January 2025
- AnyAsync(predicate) using EXISTS SQL pattern
- AllAsync(predicate) using NOT EXISTS with negated predicate
- Expression tree translation for lambda predicates
- 12 new unit tests for existence checks
- 6 integration tests in test-app
- Full documentation and examples
- **195 total tests passing** (183 + 12 new)

---

### Phase 2: Performance & Async (v1.9.0 - v1.10.0)
#### v1.9.0 - Async Streaming
**Target**: Q2 2026

**Features**:
- ✅ `IAsyncEnumerable<T>` support
- ✅ `ToAsyncEnumerable()` for large datasets
- ✅ Streaming results without loading all into memory
- ✅ Cancellation token support throughout

**Example**:
```csharp
await foreach (var user in client.AsQueryable<User>("users")
    .Where(u => u.IsActive)
    .ToAsyncEnumerable()
    .WithCancellation(cancellationToken))
{
    await ProcessUserAsync(user);
}
```

**Estimated Effort**: 2 weeks
- IAsyncEnumerable implementation
- Streaming result parsing
- Memory optimization
- Unit tests (10-15 new tests)

---

#### v1.10.0 - Query Optimization & Caching
**Target**: Q3 2026

**Features**:
- ✅ Query plan caching
- ✅ Expression tree compilation cache
- ✅ Compiled queries: `CompiledQuery.Create()`
- ✅ Query hints and optimization flags
- ✅ Performance benchmarks

**Example**:
```csharp
// Compiled query - parse expression once, execute many times
var getActiveUsers = CompiledQuery.Create(
    (ID1Client client) => client.AsQueryable<User>("users")
        .Where(u => u.IsActive)
        .OrderBy(u => u.Name)
);

var users = await getActiveUsers(client).ToListAsync();
```

**Estimated Effort**: 2-3 weeks
- Expression compilation caching
- Query plan optimization
- Benchmark suite
- Performance documentation

---

### Phase 3: LINQ Feature Complete (v2.0.0)

#### v2.0.0 - Complete LINQ Provider 🎉
**Target**: Q4 2026

**Features**:
- ✅ All standard LINQ methods supported
- ✅ Complex nested queries
- ✅ Subqueries in WHERE/SELECT/FROM
- ✅ Window functions (ROW_NUMBER, RANK, etc.)
- ✅ CTEs (Common Table Expressions)
- ✅ Full SQL feature parity

**Example**:
```csharp
// Subquery
var topCustomers = await client.AsQueryable<Customer>("customers")
    .Where(c => c.Orders.Sum(o => o.Total) > 1000)
    .Select(c => new {
        c.Name,
        TotalSpent = c.Orders.Sum(o => o.Total),
        OrderCount = c.Orders.Count()
    })
    .ToListAsync();

// Window function
var rankedProducts = await client.AsQueryable<Product>("products")
    .Select(p => new {
        p.Name,
        p.Price,
        Rank = Sql.RowNumber().Over(o => o.OrderBy(p.Price))
    })
    .ToListAsync();
```

**Estimated Effort**: 4-6 weeks
- Subquery support
- Window function API
- CTE generation
- Complex nested query handling
- Comprehensive test suite
- Migration guide from v1.x

---

## 📦 Migrations Package - Separate Timeline

### CloudflareD1.NET.Migrations

#### v1.0.0 - Core Migration Features
**Target**: Q1 2026 (Parallel with LINQ v1.6.0)

**Features**:
- ✅ Code-first migrations
- ✅ Migration versioning & history
- ✅ Up/Down migration support
- ✅ Automatic rollback on failure
- ✅ CLI tool for migration management

**Example**:
```csharp
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("email").AsString(255).Unique()
            .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Drop.Table("users");
    }
}
```

**CLI**:
```bash
dotnet d1-migrate up              # Apply pending migrations
dotnet d1-migrate down            # Rollback last migration
dotnet d1-migrate create AddUsersTable  # Generate new migration
dotnet d1-migrate status          # Show migration status
```

**Estimated Effort**: 3-4 weeks
- Migration runner infrastructure
- SQL generation for schema changes
- Version tracking table
- CLI tool development
- Documentation & examples

---

#### v1.1.0 - Advanced Schema Operations
**Target**: Q2 2026

**Features**:
- ✅ Foreign key constraints
- ✅ Indexes (unique, composite)
- ✅ Triggers
- ✅ Views
- ✅ Seed data support
- ✅ Migration dependencies

**Estimated Effort**: 2-3 weeks

---

#### v1.2.0 - Data Migrations & Transformations
**Target**: Q3 2026

**Features**:
- ✅ Data transformation migrations
- ✅ Bulk data operations
- ✅ Migration testing framework
- ✅ Dry-run mode
- ✅ Production-safe migrations

**Estimated Effort**: 2-3 weeks

---

## 🧪 Testing Package (Optional)

### CloudflareD1.NET.Testing

#### v1.0.0 - Testing Helpers
**Target**: Q2 2026

**Features**:
- ✅ In-memory SQLite test fixtures
- ✅ Mock D1Client for unit tests
- ✅ Test data builders
- ✅ Integration test helpers
- ✅ Snapshot testing for queries

**Estimated Effort**: 2 weeks

---

## 📈 Summary Timeline

### 2025 Q4
- ✅ **v1.4.0** - IQueryable Select() (DONE)
- 🎯 **v1.5.0** - GroupBy & Aggregations (NEXT)

### 2026 Q1
- **v1.6.0** - Join Support
- **Migrations v1.0.0** - Core migrations

### 2026 Q2
- **v1.7.0** - Advanced LINQ methods
- **v1.8.0** - Async streaming
- **Migrations v1.1.0** - Advanced schema
- **Testing v1.0.0** - Testing helpers

### 2026 Q3
- **v1.9.0** - Query optimization
- **Migrations v1.2.0** - Data migrations

### 2026 Q4
- **v2.0.0** - Complete LINQ provider 🎉

---

## 📊 Completion Tracker

### LINQ Features Status

| Feature | Status | Version | Effort |
|---------|--------|---------|--------|
| Basic queries | ✅ | v1.0.0 | Done |
| Expression trees | ✅ | v1.1.0 | Done |
| Select() projection | ✅ | v1.2.0 | Done |
| IQueryable<T> | ✅ | v1.3.0 | Done |
| IQueryable Select() | ✅ | v1.4.0 | Done |
| GroupBy() | 🎯 | v1.5.0 | 2-3w |
| Join() | ⏳ | v1.6.0 | 3-4w |
| Advanced LINQ | ⏳ | v1.7.0 | 2-3w |
| Async streaming | ⏳ | v1.8.0 | 2w |
| Optimization | ⏳ | v1.9.0 | 2-3w |
| Full LINQ | ⏳ | v2.0.0 | 4-6w |

**Total LINQ Effort Remaining**: ~18-24 weeks (~5-6 months)

### Migrations Status

| Feature | Status | Version | Effort |
|---------|--------|---------|--------|
| Core migrations | ⏳ | v1.0.0 | 3-4w |
| Advanced schema | ⏳ | v1.1.0 | 2-3w |
| Data migrations | ⏳ | v1.2.0 | 2-3w |

**Total Migrations Effort**: ~7-10 weeks (~2-3 months, parallel with LINQ)

---

## 🎯 Priority Order

1. **v1.5.0 GroupBy** - Most requested feature for reporting
2. **v1.6.0 Join** - Critical for multi-table queries
3. **Migrations v1.0.0** - Schema management is essential
4. **v1.7.0 Advanced LINQ** - Complete common use cases
5. **v1.8.0 Streaming** - Performance for large datasets
6. **v1.9.0 Optimization** - Production performance
7. **v2.0.0 Full LINQ** - Feature complete

---

## 💡 Community Input

Want a feature prioritized? Open an issue or discussion:
- 🐛 [Issue Tracker](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 💬 [Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)

---

**Last Updated**: October 26, 2025  
**Current Version**: v1.4.0  
**Next Release**: v1.5.0 (GroupBy & Aggregations)
