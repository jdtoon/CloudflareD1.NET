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

    Console.WriteLine("Step 8: Getting total count...");
    var countResult = await client.QueryAsync("SELECT COUNT(*) as total FROM test_users");
    if (countResult.Results != null && countResult.Results.Count > 0)
    {
        Console.WriteLine($"✓ Total users in database: {countResult.Results[0]["total"]}\n");
    }

    Console.WriteLine("Step 9: Cleaning up test data (optional - comment out if you want to keep)...");
    var deleteResult = await client.ExecuteAsync(
        "DELETE FROM test_users WHERE email LIKE @pattern",
        new { pattern = "%@example.com" }
    );
    Console.WriteLine($"✓ Deleted {deleteResult.Meta?.Changes} test row(s)\n");

    Console.WriteLine("========================================");
    Console.WriteLine("🎉 ALL TESTS PASSED SUCCESSFULLY!");
    Console.WriteLine("========================================");
    Console.WriteLine("\nYour CloudflareD1.NET package (with LINQ extensions) is working correctly with Cloudflare D1!");
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
