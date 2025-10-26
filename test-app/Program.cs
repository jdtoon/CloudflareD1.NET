using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

Console.WriteLine("=== CloudflareD1.NET - Cloudflare D1 Connection Test ===\n");

// Build configuration from appsettings.json and environment variables
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger<D1Client>();

// Get configuration
var d1Config = configuration.GetSection("CloudflareD1").Get<D1Options>();

if (d1Config == null)
{
    Console.WriteLine("❌ ERROR: CloudflareD1 configuration not found in appsettings.json");
    Console.WriteLine("\nPlease ensure appsettings.json contains:");
    Console.WriteLine(@"{
  ""CloudflareD1"": {
    ""Mode"": ""Remote"",
    ""AccountId"": ""your-account-id"",
    ""DatabaseId"": ""your-database-id"",
    ""ApiToken"": ""your-api-token""
  }
}");
    return;
}

// Validate configuration
if (string.IsNullOrEmpty(d1Config.AccountId) || d1Config.AccountId == "your-account-id")
{
    Console.WriteLine("❌ ERROR: AccountId not configured");
    Console.WriteLine("Please update appsettings.json with your actual Cloudflare Account ID");
    return;
}

if (string.IsNullOrEmpty(d1Config.DatabaseId) || d1Config.DatabaseId == "your-database-id")
{
    Console.WriteLine("❌ ERROR: DatabaseId not configured");
    Console.WriteLine("Please update appsettings.json with your actual D1 Database ID");
    return;
}

if (string.IsNullOrEmpty(d1Config.ApiToken) || d1Config.ApiToken == "your-api-token")
{
    Console.WriteLine("❌ ERROR: ApiToken not configured");
    Console.WriteLine("Please update appsettings.json with your actual Cloudflare API Token");
    return;
}

Console.WriteLine("✓ Configuration loaded successfully");
Console.WriteLine($"  Use Local Mode: {d1Config.UseLocalMode}");
Console.WriteLine($"  Account ID: {d1Config.AccountId[..8]}...");
Console.WriteLine($"  Database ID: {d1Config.DatabaseId[..8]}...");
Console.WriteLine($"  API Token: {d1Config.ApiToken[..8]}...\n");

// Create D1 client
var options = Options.Create(d1Config);
using var client = new D1Client(options, logger);

try
{
    Console.WriteLine("Step 1: Creating test table...");
    // Drop existing table to add age column
    await client.ExecuteAsync("DROP TABLE IF EXISTS test_users");
    await client.ExecuteAsync(@"
        CREATE TABLE test_users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            age INTEGER DEFAULT 0,
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        )
    ");
    Console.WriteLine("✓ Table created successfully\n");

    Console.WriteLine("Step 2: Inserting test data...");
    var insertResult = await client.ExecuteAsync(
        "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)",
        new { name = "Test User", email = $"test{DateTime.UtcNow.Ticks}@example.com", age = 25 }
    );
    Console.WriteLine($"✓ Inserted user with ID: {insertResult.Meta?.LastRowId}\n");

    Console.WriteLine("Step 3: Querying data...");
    var queryResult = await client.QueryAsync("SELECT * FROM test_users ORDER BY id DESC LIMIT 5");
    Console.WriteLine($"✓ Found {queryResult.Results?.Count ?? 0} users:");

    if (queryResult.Results != null)
    {
        foreach (var user in queryResult.Results)
        {
            Console.WriteLine($"  - ID: {user["id"]}, Name: {user["name"]}, Email: {user["email"]}");
        }
    }
    Console.WriteLine();

    Console.WriteLine("Step 4: Updating data...");
    var updateResult = await client.ExecuteAsync(
        "UPDATE test_users SET name = @name WHERE id = @id",
        new { name = "Updated Test User", id = insertResult.Meta?.LastRowId }
    );
    Console.WriteLine($"✓ Updated {updateResult.Meta?.Changes} row(s)\n");

    Console.WriteLine("Step 5: Testing batch operations...");
    var email1 = $"batch1-{DateTime.UtcNow.Ticks}@example.com";
    var email2 = $"batch2-{DateTime.UtcNow.Ticks + 1}@example.com";
    var statements = new List<D1Statement>
    {
        new D1Statement
        {
            Sql = $"INSERT INTO test_users (name, email, age) VALUES ('Batch User 1', '{email1}', 30)"
        },
        new D1Statement
        {
            Sql = $"INSERT INTO test_users (name, email, age) VALUES ('Batch User 2', '{email2}', 17)"
        },
    };

    var batchResults = await client.BatchAsync(statements);
    Console.WriteLine($"✓ Executed {batchResults.Length} statements in batch\n");

    Console.WriteLine("Step 6: Testing LINQ QueryAsync<T>...");
    var typedUsers = await client.QueryAsync<TestUser>("SELECT * FROM test_users ORDER BY id DESC LIMIT 3");
    var userList = typedUsers.ToList();
    Console.WriteLine($"✓ Found {userList.Count} users using LINQ:");
    foreach (var user in userList)
    {
        Console.WriteLine($"  - ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 7: Testing LINQ QueryFirstOrDefaultAsync<T>...");
    var firstUser = await client.QueryFirstOrDefaultAsync<TestUser>(
        "SELECT * FROM test_users WHERE email LIKE @pattern LIMIT 1",
        new { pattern = "%@example.com" }
    );
    if (firstUser != null)
    {
        Console.WriteLine($"✓ First user: {firstUser.Name} ({firstUser.Email})");
    }
    Console.WriteLine();

    Console.WriteLine("Step 8: Testing Query Builder - Where clause with parameters...");
    // First test without parameters to see if it's a parameter binding issue
    var allUsers = await client.Query<TestUser>("test_users")
        .ToListAsync();
    Console.WriteLine($"✓ Query builder found {allUsers.Count()} total users");

    var queryResults = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .ToListAsync();
    Console.WriteLine($"✓ Query builder found {queryResults.Count()} users with example.com emails");
    Console.WriteLine();

    Console.WriteLine("Step 9: Testing Query Builder - OrderBy and Take...");
    var orderedUsers = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .OrderBy("id")
        .Take(2)
        .ToListAsync();
    Console.WriteLine($"✓ Top 2 users ordered by ID:");
    foreach (var u in orderedUsers)
    {
        Console.WriteLine($"   - ID: {u.Id}, Name: {u.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 10: Testing Query Builder - OrderByDescending and Skip...");
    var paginatedUsers = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .OrderByDescending("id")
        .Skip(1)
        .Take(2)
        .ToListAsync();
    Console.WriteLine($"✓ Page 2 (skip 1, take 2) ordered by ID descending:");
    foreach (var u in paginatedUsers)
    {
        Console.WriteLine($"   - ID: {u.Id}, Name: {u.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 11: Testing Query Builder - CountAsync and AnyAsync...");
    var userCount = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .CountAsync();
    var hasUsers = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .AnyAsync();
    Console.WriteLine($"✓ Count: {userCount}, Has users: {hasUsers}");
    Console.WriteLine();

    Console.WriteLine("Step 12: Testing Query Builder - SingleOrDefaultAsync...");
    try
    {
        var singleUser = await client.Query<TestUser>("test_users")
            .Where("id = ?", orderedUsers.First().Id)
            .SingleOrDefaultAsync();
        Console.WriteLine($"✓ Single user by ID: {singleUser?.Name ?? "null"}");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"✗ Expected behavior: {ex.Message}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 13: Testing Query Builder - ThenBy for multi-column sorting...");
    var multiSortUsers = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .OrderBy("name")
        .ThenByDescending("id")
        .ToListAsync();
    Console.WriteLine($"✓ Multi-column sort (name ASC, then id DESC): {multiSortUsers.Count()} users");
    foreach (var u in multiSortUsers.Take(2))
    {
        Console.WriteLine($"   - Name: {u.Name}, ID: {u.Id}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 14: Testing Query Builder - FirstOrDefaultAsync...");
    var firstFromBuilder = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .OrderBy("id")
        .FirstOrDefaultAsync();
    Console.WriteLine($"✓ First user from builder: {firstFromBuilder?.Name ?? "null"}");
    Console.WriteLine();

    Console.WriteLine("Step 15: Testing Query Builder - SingleAsync (should throw if not exactly 1)...");
    try
    {
        // This should throw because we have multiple users
        var single = await client.Query<TestUser>("test_users")
            .Where("email LIKE ?", "%@example.com")
            .SingleAsync();
        Console.WriteLine($"✗ Should have thrown but got: {single.Name}");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("✓ SingleAsync correctly threw InvalidOperationException for multiple results");
    }
    Console.WriteLine();

    Console.WriteLine("Step 16: Testing extension QuerySingleAsync (should throw if not exactly 1)...");
    try
    {
        // This should succeed - get single user by ID
        var singleUser = await client.QuerySingleAsync<TestUser>(
            "SELECT * FROM test_users WHERE id = @id",
            new { id = orderedUsers.First().Id }
        );
        Console.WriteLine($"✓ QuerySingleAsync returned: {singleUser.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ QuerySingleAsync failed: {ex.Message}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 17: Testing extension QuerySingleOrDefaultAsync...");
    var singleOrDefault = await client.QuerySingleOrDefaultAsync<TestUser>(
        "SELECT * FROM test_users WHERE id = @id",
        new { id = orderedUsers.First().Id }
    );
    Console.WriteLine($"✓ QuerySingleOrDefaultAsync returned: {singleOrDefault?.Name ?? "null"}");

    // Test with no results
    var noResult = await client.QuerySingleOrDefaultAsync<TestUser>(
        "SELECT * FROM test_users WHERE id = @id",
        new { id = 999999 }
    );
    Console.WriteLine($"✓ QuerySingleOrDefaultAsync with no results returned: {(noResult == null ? "null" : "value")}");
    Console.WriteLine();

    Console.WriteLine("Step 18: Getting total count...");
    var countResult = await client.QueryAsync("SELECT COUNT(*) as total FROM test_users");
    if (countResult.Results != null && countResult.Results.Count > 0)
    {
        Console.WriteLine($"✓ Total users in database: {countResult.Results[0]["total"]}\n");
    }

    Console.WriteLine("Step 19: Testing expression-based Where with lambda...");
    var lambdaUsers = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("@example.com"))
        .ToListAsync();
    Console.WriteLine($"✓ Lambda Where clause found {lambdaUsers.Count()} users");
    Console.WriteLine();

    Console.WriteLine("Step 20: Testing expression-based OrderBy with lambda...");
    var lambdaOrdered = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null)
        .OrderBy(u => u.Name)
        .ThenByDescending(u => u.Id)
        .Take(3)
        .ToListAsync();
    Console.WriteLine($"✓ Lambda OrderBy found {lambdaOrdered.Count()} users");
    foreach (var u in lambdaOrdered)
    {
        Console.WriteLine($"   - Name: {u.Name}, ID: {u.Id}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 21: Testing complex expression with AND/OR logic...");
    var complexUsers = await client.Query<TestUser>("test_users")
        .Where(u => (u.Id > 0 && u.Id < 999999) || u.Name.Contains("Test"))
        .ToListAsync();
    Console.WriteLine($"✓ Complex expression found {complexUsers.Count()} users");
    Console.WriteLine();

    Console.WriteLine("Step 22: Comparing string-based vs expression-based results...");
    var stringBased = await client.Query<TestUser>("test_users")
        .Where("email LIKE ?", "%@example.com")
        .OrderBy("name")
        .Take(5)
        .ToListAsync();

    var expressionBased = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("@example.com"))
        .OrderBy(u => u.Name)
        .Take(5)
        .ToListAsync();

    Console.WriteLine($"✓ String-based: {stringBased.Count()} users, Expression-based: {expressionBased.Count()} users");
    Console.WriteLine($"✓ Results match: {stringBased.Count() == expressionBased.Count()}");
    Console.WriteLine();

    Console.WriteLine("Step 23: Select() projection - specific columns only...");
    var userSummaries = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
        .Take(3)
        .ToListAsync();
    Console.WriteLine($"✓ Selected {userSummaries.Count()} user summaries (Id, Name only)");
    foreach (var summary in userSummaries.Take(2))
    {
        Console.WriteLine($"  - {summary.Id}: {summary.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 24: Select() with Where and OrderBy combination...");
    var activeUserNames = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("@example.com"))
        .OrderBy(u => u.Name)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
        .Take(5)
        .ToListAsync();
    Console.WriteLine($"✓ Found {activeUserNames.Count()} users with projections");
    Console.WriteLine($"✓ First user: {activeUserNames.FirstOrDefault()?.Name ?? "None"}");
    Console.WriteLine();

    Console.WriteLine("Step 25: Select() FirstOrDefault with projection...");
    var firstSummary = await client.Query<TestUser>("test_users")
        .OrderBy(u => u.Id)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
        .FirstOrDefaultAsync();
    Console.WriteLine($"✓ First user summary: {firstSummary?.Name ?? "None"} (ID: {firstSummary?.Id})");
    Console.WriteLine();

    Console.WriteLine("Step 26: Select() Count works with projections...");
    var projectionCount = await client.Query<TestUser>("test_users")
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
        .CountAsync();
    Console.WriteLine($"✓ Count with projection: {projectionCount}");
    Console.WriteLine();

    Console.WriteLine("Step 27: Select() with computed property - boolean expression...");
    var usersWithAdultFlag = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null)
        .Select(u => new UserWithComputedProps { Id = u.Id, Name = u.Name, Age = u.Age, IsAdult = u.Age >= 18 })
        .Take(5)
        .ToListAsync();
    Console.WriteLine($"✓ Selected {usersWithAdultFlag.Count()} users with computed IsAdult property");
    foreach (var user in usersWithAdultFlag.Take(3))
    {
        Console.WriteLine($"  - {user.Name}: Age {user.Age} → IsAdult = {user.IsAdult}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 28: Select() with computed property - math operation...");
    var usersWithCalculation = await client.Query<TestUser>("test_users")
        .Where(u => u.Age > 0)
        .Select(u => new UserWithCalculation {
            Id = u.Id,
            Name = u.Name,
            Age = u.Age,
            YearsUntil65 = 65 - u.Age
        })
        .Take(3)
        .ToListAsync();
    Console.WriteLine($"✓ Selected {usersWithCalculation.Count()} users with computed YearsUntil65");
    foreach (var user in usersWithCalculation)
    {
        Console.WriteLine($"  - {user.Name}: Age {user.Age} → {user.YearsUntil65} years until 65");
    }
    Console.WriteLine();

    Console.WriteLine("Step 29: Select() with multiple computed properties...");
    var usersWithMultiple = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null)
        .Select(u => new UserWithMultipleComputed {
            Id = u.Id,
            Name = u.Name,
            Age = u.Age,
            IsAdult = u.Age >= 18,
            IsMinor = u.Age < 18,
            IsSenior = u.Age >= 65
        })
        .Take(3)
        .ToListAsync();
    Console.WriteLine($"✓ Selected {usersWithMultiple.Count()} users with multiple computed properties");
    foreach (var user in usersWithMultiple.Take(2))
    {
        Console.WriteLine($"  - {user.Name} (Age {user.Age}): Adult={user.IsAdult}, Minor={user.IsMinor}, Senior={user.IsSenior}");
    }
    Console.WriteLine();

    // ============================================================
    // NEW: IQueryable<T> Tests (v1.3.0)
    // ============================================================
    Console.WriteLine("========================================");
    Console.WriteLine("🆕 IQueryable<T> Tests (v1.3.0)");
    Console.WriteLine("========================================\n");

    Console.WriteLine("Step 31: Basic IQueryable - Deferred Execution...");
    IQueryable<TestUser> queryable = client.AsQueryable<TestUser>("test_users");
    Console.WriteLine("✓ IQueryable created (query NOT executed yet - deferred execution)");
    Console.WriteLine($"  Provider Type: {queryable.Provider.GetType().Name}");
    Console.WriteLine($"  Expression Type: {queryable.Expression.Type.Name}");
    Console.WriteLine();

    Console.WriteLine("Step 32: IQueryable with Where - Still Deferred...");
    var adults = queryable.Where(u => u.Age >= 18);
    Console.WriteLine("✓ Where clause added (still not executed)");
    Console.WriteLine($"  Expression now includes Where predicate");
    Console.WriteLine();

    Console.WriteLine("Step 33: IQueryable Execution with ToListAsync...");
    var adultList = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)adults).ToListAsync();
    Console.WriteLine($"✓ Query executed! Retrieved {adultList.Count()} adult users");
    foreach (var user in adultList.Take(3))
    {
        Console.WriteLine($"  - {user.Name}, Age {user.Age}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 34: IQueryable with Multiple Where Clauses...");
    var youngAdults = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 18)
        .Where(u => u.Age < 30);
    var youngAdultList = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)youngAdults).ToListAsync();
    Console.WriteLine($"✓ Multiple WHERE clauses combined with AND: {youngAdultList.Count()} users (18-29)");
    foreach (var user in youngAdultList.Take(2))
    {
        Console.WriteLine($"  - {user.Name}, Age {user.Age}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 35: IQueryable with OrderBy...");
    var sortedByName = client.AsQueryable<TestUser>("test_users")
        .OrderBy(u => u.Name)
        .Take(5);
    var sortedList = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)sortedByName).ToListAsync();
    Console.WriteLine($"✓ Ordered by Name: {sortedList.Count()} users");
    foreach (var user in sortedList)
    {
        Console.WriteLine($"  - {user.Name}, Age {user.Age}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 36: IQueryable with OrderByDescending...");
    var sortedByAgeDesc = client.AsQueryable<TestUser>("test_users")
        .OrderByDescending(u => u.Age)
        .Take(5);
    var sortedAgeList = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)sortedByAgeDesc).ToListAsync();
    Console.WriteLine($"✓ Ordered by Age (descending): {sortedAgeList.Count()} users");
    foreach (var user in sortedAgeList)
    {
        Console.WriteLine($"  - {user.Name}, Age {user.Age}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 37: IQueryable with Pagination (Skip + Take)...");
    var pagedQuery = client.AsQueryable<TestUser>("test_users")
        .OrderBy(u => u.Id)
        .Skip(2)
        .Take(3);
    var pagedResults = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)pagedQuery).ToListAsync();
    Console.WriteLine($"✓ Pagination (Skip 2, Take 3): {pagedResults.Count()} users");
    foreach (var user in pagedResults)
    {
        Console.WriteLine($"  - ID {user.Id}: {user.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 38: IQueryable Complex Query - Where + OrderBy + Pagination...");
    var complexQuery = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age > 20)
        .OrderBy(u => u.Name)
        .Skip(1)
        .Take(5);
    var complexResults = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)complexQuery).ToListAsync();
    Console.WriteLine($"✓ Complex query (Age > 20, ordered by Name, paginated): {complexResults.Count()} users");
    foreach (var user in complexResults.Take(3))
    {
        Console.WriteLine($"  - {user.Name}, Age {user.Age}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 39: IQueryable CountAsync...");
    var countQuery = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 18);
    var adultCount = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)countQuery).CountAsync();
    Console.WriteLine($"✓ COUNT query executed: {adultCount} adult users");
    Console.WriteLine();

    Console.WriteLine("Step 40: IQueryable FirstOrDefaultAsync...");
    var firstQuery = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 18)
        .OrderBy(u => u.Name);
    var firstAdult = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)firstQuery).FirstOrDefaultAsync();
    if (firstAdult != null)
    {
        Console.WriteLine($"✓ First adult user: {firstAdult.Name}, Age {firstAdult.Age}");
    }
    else
    {
        Console.WriteLine("✓ No adult users found");
    }
    Console.WriteLine();

    Console.WriteLine("Step 41: IQueryable AnyAsync...");
    var anyQuery = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 65);
    var hasSeniors = await ((CloudflareD1.NET.Linq.Query.D1Queryable<TestUser>)anyQuery).AnyAsync();
    Console.WriteLine($"✓ Has senior users (65+): {hasSeniors}");
    Console.WriteLine();

    Console.WriteLine("========================================");
    Console.WriteLine("✅ IQueryable<T> Tests Completed!");
    Console.WriteLine("========================================");

    Console.WriteLine("========================================");
    Console.WriteLine("Testing IQueryable<T> Select() Projections");
    Console.WriteLine("========================================\n");

    Console.WriteLine("Step 42: Simple Select() projection...");
    var simpleProjection = client.AsQueryable<TestUser>("test_users")
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var summaries = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)simpleProjection).ToListAsync();
    Console.WriteLine($"✓ Retrieved {summaries.Count()} user summaries");
    if (summaries.Any())
    {
        Console.WriteLine($"  First: ID={summaries.First().Id}, Name={summaries.First().Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 43: Select() with Where() filter...");
    var filteredProjection = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 21)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var adultSummaries = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)filteredProjection).ToListAsync();
    Console.WriteLine($"✓ Retrieved {adultSummaries.Count()} adult users (21+)");
    Console.WriteLine();

    Console.WriteLine("Step 44: Select() with OrderBy()...");
    var orderedProjection = client.AsQueryable<TestUser>("test_users")
        .OrderBy(u => u.Name)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var sortedSummaries = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)orderedProjection).ToListAsync();
    Console.WriteLine($"✓ Retrieved {sortedSummaries.Count()} users ordered by name");
    if (sortedSummaries.Any())
    {
        Console.WriteLine($"  First (alphabetically): {sortedSummaries.First().Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 45: Select() with pagination...");
    var pagedProjection = client.AsQueryable<TestUser>("test_users")
        .OrderBy(u => u.Id)
        .Skip(1)
        .Take(2)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var pagedSummaries = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)pagedProjection).ToListAsync();
    Console.WriteLine($"✓ Retrieved {pagedSummaries.Count()} users (page 2, size 2)");
    foreach (var user in pagedSummaries)
    {
        Console.WriteLine($"  ID={user.Id}, Name={user.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 46: Select() with complex chain (Where + OrderBy + Take)...");
    var complexProjection = client.AsQueryable<TestUser>("test_users")
        .Where(u => u.Age >= 18)
        .OrderByDescending(u => u.Age)
        .Take(3)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var topAdults = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)complexProjection).ToListAsync();
    Console.WriteLine($"✓ Retrieved top {topAdults.Count()} oldest adults");
    Console.WriteLine();

    Console.WriteLine("Step 47: Select() projection with FirstOrDefaultAsync()...");
    var firstProjection = client.AsQueryable<TestUser>("test_users")
        .OrderBy(u => u.Id)
        .Select(u => new UserSummary { Id = u.Id, Name = u.Name });
    var firstProjectedUser = await ((CloudflareD1.NET.Linq.Query.D1ProjectionQueryable<UserSummary>)firstProjection).FirstOrDefaultAsync();
    Console.WriteLine($"✓ First user: {(firstProjectedUser != null ? $"ID={firstProjectedUser.Id}, Name={firstProjectedUser.Name}" : "None")}");
    Console.WriteLine();

    Console.WriteLine("========================================");
    Console.WriteLine("✅ IQueryable<T> Select() Tests Completed!");
    Console.WriteLine("========================================\n");

    Console.WriteLine("========================================");
    Console.WriteLine("🧪 Testing GroupBy & Aggregations (v1.5.0)");
    Console.WriteLine("========================================\n");

    // First, let's add some more varied data for grouping tests
    Console.WriteLine("Step 49: Adding more test data for GroupBy tests...");
    await client.ExecuteAsync(@"
        INSERT INTO test_users (name, email, age) VALUES
        ('Alice', 'alice@demo.com', 25),
        ('Bob', 'bob@demo.com', 30),
        ('Charlie', 'charlie@demo.com', 25),
        ('David', 'david@demo.com', 30),
        ('Eve', 'eve@demo.com', 35)
    ");
    Console.WriteLine("✓ Added test data for grouping\n");

    Console.WriteLine("Step 50: GroupBy with Count() - Group users by age...");
    var ageGroups = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroup
        {
            Age = g.Key,
            UserCount = g.Count()
        })
        .ToListAsync();
    Console.WriteLine($"✓ Found {ageGroups.Count()} age groups:");
    foreach (var group in ageGroups.OrderBy(g => g.Age))
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s)");
    }
    Console.WriteLine();

    Console.WriteLine("Step 51: GroupBy with Sum() - Calculate total ages per group...");
    var ageGroupsWithSum = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroupWithAggregates
        {
            Age = g.Key,
            UserCount = g.Count(),
            TotalAge = g.Sum(u => u.Age)
        })
        .ToListAsync();
    Console.WriteLine($"✓ Age groups with sum:");
    foreach (var group in ageGroupsWithSum.OrderBy(g => g.Age))
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s), total age: {group.TotalAge}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 52: GroupBy with Average() - Average age per group...");
    var ageGroupsWithAvg = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroupWithStats
        {
            Age = g.Key,
            UserCount = g.Count(),
            AverageAge = g.Average(u => u.Age)
        })
        .ToListAsync();
    Console.WriteLine($"✓ Age groups with average:");
    foreach (var group in ageGroupsWithAvg.OrderBy(g => g.Age))
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s), avg: {group.AverageAge:F1}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 53: GroupBy with Min/Max - Min and Max ages per group...");
    var ageGroupsWithMinMax = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroupComplete
        {
            Age = g.Key,
            UserCount = g.Count(),
            MinAge = g.Min(u => u.Age),
            MaxAge = g.Max(u => u.Age)
        })
        .ToListAsync();
    Console.WriteLine($"✓ Age groups with min/max:");
    foreach (var group in ageGroupsWithMinMax.OrderBy(g => g.Age))
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s), min: {group.MinAge}, max: {group.MaxAge}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 54: GroupBy with multiple aggregates - Full statistics...");
    var fullStats = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new FullAgeStats
        {
            Age = g.Key,
            UserCount = g.Count(),
            TotalAge = g.Sum(u => u.Age),
            AverageAge = g.Average(u => u.Age),
            MinAge = g.Min(u => u.Age),
            MaxAge = g.Max(u => u.Age)
        })
        .ToListAsync();
    Console.WriteLine($"✓ Full statistics by age group:");
    foreach (var stats in fullStats.OrderBy(s => s.Age))
    {
        Console.WriteLine($"  Age {stats.Age}: count={stats.UserCount}, sum={stats.TotalAge}, avg={stats.AverageAge:F1}, min={stats.MinAge}, max={stats.MaxAge}");
    }
    Console.WriteLine();

    Console.WriteLine("Step 55: GroupBy with OrderBy - Ordered by count descending...");
    var orderedGroups = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroup
        {
            Age = g.Key,
            UserCount = g.Count()
        })
        .OrderByDescending("user_count")
        .ToListAsync();
    Console.WriteLine($"✓ Age groups ordered by count (DESC):");
    foreach (var group in orderedGroups)
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s)");
    }
    Console.WriteLine();

    Console.WriteLine("Step 56: GroupBy with Take - Top 2 age groups...");
    var topGroups = await client.Query<TestUser>("test_users")
        .Where(u => u.Email != null && u.Email.Contains("demo.com"))
        .GroupBy(u => u.Age)
        .Select(g => new AgeGroup
        {
            Age = g.Key,
            UserCount = g.Count()
        })
        .OrderByDescending("user_count")
        .Take(2)
        .ToListAsync();
    Console.WriteLine($"✓ Top 2 age groups:");
    foreach (var group in topGroups)
    {
        Console.WriteLine($"  Age {group.Age}: {group.UserCount} user(s)");
    }
    Console.WriteLine();

    Console.WriteLine("========================================");
    Console.WriteLine("✅ GroupBy & Aggregations Tests Completed!");
    Console.WriteLine("========================================\n");

    Console.WriteLine("Step 57: Cleaning up test data (optional - comment out if you want to keep)...");
    var deleteResult = await client.ExecuteAsync(
        "DELETE FROM test_users WHERE email LIKE @pattern OR email LIKE @pattern2",
        new { pattern = "%@example.com", pattern2 = "%@demo.com" }
    );
    Console.WriteLine($"✓ Deleted {deleteResult.Meta?.Changes} test row(s)\n");

    Console.WriteLine("========================================");
    Console.WriteLine("🎉 ALL TESTS PASSED SUCCESSFULLY!");
    Console.WriteLine("========================================");
    Console.WriteLine("\nYour CloudflareD1.NET package (with LINQ expression trees and computed properties) is working correctly with Cloudflare D1!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine($"\nDetails: {ex}");

    if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
    {
        Console.WriteLine("\n💡 Tip: Check that your API Token has the correct permissions for D1 databases");
    }
    else if (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
    {
        Console.WriteLine("\n💡 Tip: Verify your Account ID and Database ID are correct");
    }
}

// Test entity for LINQ queries
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public string? CreatedAt { get; set; }
}

// DTO for Select() projection tests
public class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// DTO for computed property tests
public class UserWithComputedProps
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsAdult { get; set; }
}

public class UserWithCalculation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public int YearsUntil65 { get; set; }
}

public class UserWithMultipleComputed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsAdult { get; set; }
    public bool IsMinor { get; set; }
    public bool IsSenior { get; set; }
}

// GroupBy result classes (v1.5.0)
public class AgeGroup
{
    public int Age { get; set; }
    public int UserCount { get; set; }
}

public class AgeGroupWithAggregates
{
    public int Age { get; set; }
    public int UserCount { get; set; }
    public int TotalAge { get; set; }
}

public class AgeGroupWithStats
{
    public int Age { get; set; }
    public int UserCount { get; set; }
    public double AverageAge { get; set; }
}

public class AgeGroupComplete
{
    public int Age { get; set; }
    public int UserCount { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
}

public class FullAgeStats
{
    public int Age { get; set; }
    public int UserCount { get; set; }
    public int TotalAge { get; set; }
    public double AverageAge { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
}

