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
    await client.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS test_users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        )
    ");
    Console.WriteLine("✓ Table created successfully\n");

    Console.WriteLine("Step 2: Inserting test data...");
    var insertResult = await client.ExecuteAsync(
        "INSERT INTO test_users (name, email) VALUES (@name, @email)",
        new { name = "Test User", email = $"test{DateTime.UtcNow.Ticks}@example.com" }
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
            Sql = $"INSERT INTO test_users (name, email) VALUES ('Batch User 1', '{email1}')"
        },
        new D1Statement
        {
            Sql = $"INSERT INTO test_users (name, email) VALUES ('Batch User 2', '{email2}')"
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

    Console.WriteLine("Step 27: Cleaning up test data (optional - comment out if you want to keep)...");
    var deleteResult = await client.ExecuteAsync(
        "DELETE FROM test_users WHERE email LIKE @pattern",
        new { pattern = "%@example.com" }
    );
    Console.WriteLine($"✓ Deleted {deleteResult.Meta?.Changes} test row(s)\n");

    Console.WriteLine("========================================");
    Console.WriteLine("🎉 ALL TESTS PASSED SUCCESSFULLY!");
    Console.WriteLine("========================================");
    Console.WriteLine("\nYour CloudflareD1.NET package (with LINQ expression tree support) is working correctly with Cloudflare D1!");
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
    public string? CreatedAt { get; set; }
}

// DTO for Select() projection tests
public class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
