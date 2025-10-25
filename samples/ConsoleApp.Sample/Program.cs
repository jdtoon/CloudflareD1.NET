using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Example: Using Cloudflare D1 in Local SQLite Mode
Console.WriteLine("=== Cloudflare D1.NET Console Sample ===\n");

// Configure D1 options for local mode
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "sample.db"
});

// Create a simple logger
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<D1Client>();

// Create D1 client
using var client = new D1Client(options, logger);

Console.WriteLine("1. Creating a table...");
await client.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT NOT NULL UNIQUE,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
");
Console.WriteLine("✓ Table created\n");

Console.WriteLine("2. Inserting users...");
var insertResult = await client.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "John Doe", email = "john@example.com" }
);
Console.WriteLine($"✓ Inserted user with ID: {insertResult.Meta?.LastRowId}\n");

await client.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "Jane Smith", email = "jane@example.com" }
);

Console.WriteLine("3. Querying users...");
var queryResult = await client.QueryAsync("SELECT * FROM users ORDER BY id");
Console.WriteLine($"✓ Found {queryResult.Results?.Count} users:");
foreach (var user in queryResult.Results ?? new())
{
    Console.WriteLine($"   - ID: {user["id"]}, Name: {user["name"]}, Email: {user["email"]}");
}
Console.WriteLine();

Console.WriteLine("4. Updating a user...");
await client.ExecuteAsync(
    "UPDATE users SET name = @name WHERE email = @email",
    new { name = "John Updated", email = "john@example.com" }
);
Console.WriteLine("✓ User updated\n");

Console.WriteLine("5. Using batch operations (transaction)...");
var statements = new List<D1Statement>
{
    new() { Sql = "INSERT INTO users (name, email) VALUES (@name, @email)", Params = new { name = "Alice", email = "alice@example.com" } },
    new() { Sql = "INSERT INTO users (name, email) VALUES (@name, @email)", Params = new { name = "Bob", email = "bob@example.com" } },
    new() { Sql = "SELECT COUNT(*) as count FROM users" }
};

var batchResults = await client.BatchAsync(statements);
Console.WriteLine($"✓ Batch executed: {batchResults.Length} statements");
Console.WriteLine($"   Total users: {batchResults[2].Results?[0]["count"]}\n");

Console.WriteLine("6. Final query with parameters...");
var searchResult = await client.QueryAsync(
    "SELECT * FROM users WHERE name LIKE @search ORDER BY id",
    new { search = "%John%" }
);
Console.WriteLine($"✓ Found {searchResult.Results?.Count} matching users:");
foreach (var user in searchResult.Results ?? new())
{
    Console.WriteLine($"   - Name: {user["name"]}, Email: {user["email"]}");
}

Console.WriteLine("\n✓ Sample completed successfully!");
Console.WriteLine("\nNote: Switch to remote Cloudflare D1 mode by setting:");
Console.WriteLine("  - UseLocalMode = false");
Console.WriteLine("  - AccountId = \"your-account-id\"");
Console.WriteLine("  - DatabaseId = \"your-database-id\"");
Console.WriteLine("  - ApiToken = \"your-api-token\"");
