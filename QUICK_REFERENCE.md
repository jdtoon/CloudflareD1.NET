# CloudflareD1.NET Quick Reference

A quick reference guide for common operations with CloudflareD1.NET.

## Installation

```bash
dotnet add package CloudflareD1.NET
```

## Setup

### Local Mode (Development)
```csharp
builder.Services.AddCloudflareD1Local("app.db");
```

### Remote Mode (Production)
```csharp
builder.Services.AddCloudflareD1(options =>
{
    options.Mode = D1Mode.Remote;
    options.AccountId = "your-account-id";
    options.DatabaseId = "your-database-id";
    options.ApiToken = "your-api-token";
});
```

### From Configuration
```csharp
builder.Services.AddCloudflareD1(
    builder.Configuration.GetSection("CloudflareD1")
);
```

## Basic Operations

### Query Data
```csharp
var result = await d1.QueryAsync("SELECT * FROM users");
foreach (var row in result.Results)
{
    Console.WriteLine($"Name: {row["name"]}");
}
```

### Execute Commands
```csharp
var result = await d1.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES ('John', 'john@example.com')"
);
Console.WriteLine($"Rows affected: {result.Meta.Changes}");
```

### Parameterized Queries
```csharp
var result = await d1.QueryAsync(
    "SELECT * FROM users WHERE email = @email",
    new { email = "john@example.com" }
);
```

### Batch Operations
```csharp
var statements = new[]
{
    new D1Statement("INSERT INTO users (name) VALUES ('Alice')"),
    new D1Statement("INSERT INTO users (name) VALUES ('Bob')"),
    new D1Statement("UPDATE stats SET count = count + 1")
};

var results = await d1.BatchAsync(statements);
```

## Common Patterns

### Create Table
```csharp
await d1.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT UNIQUE,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP
    )
");
```

### Insert and Get ID
```csharp
var result = await d1.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "John", email = "john@example.com" }
);
var newId = result.Meta.LastRowId;
```

### Update Records
```csharp
var result = await d1.ExecuteAsync(
    "UPDATE users SET name = @name WHERE id = @id",
    new { name = "Jane", id = 1 }
);
Console.WriteLine($"Updated {result.Meta.Changes} rows");
```

### Delete Records
```csharp
var result = await d1.ExecuteAsync(
    "DELETE FROM users WHERE id = @id",
    new { id = 1 }
);
```

### Count Records
```csharp
var result = await d1.QueryAsync("SELECT COUNT(*) as count FROM users");
var count = result.Results[0]["count"];
```

## Advanced Features

### Time Travel Queries
```csharp
var timestamp = DateTime.UtcNow.AddDays(-1);
var result = await d1.QueryAsync(
    "SELECT * FROM users",
    timestamp: timestamp
);
```

### Transaction with Batch
```csharp
var statements = new[]
{
    new D1Statement("UPDATE accounts SET balance = balance - @amount WHERE id = @from", 
        new { amount = 100, from = 1 }),
    new D1Statement("UPDATE accounts SET balance = balance + @amount WHERE id = @to", 
        new { amount = 100, to = 2 })
};

var results = await d1.BatchAsync(statements);
```

### Database Management
```csharp
// List databases
var databases = await d1.ListDatabasesAsync();

// Get database info
var info = await d1.GetDatabaseAsync("database-id");

// Create database
var newDb = await d1.CreateDatabaseAsync("my-new-database");
```

## ASP.NET Core Integration

### Dependency Injection
```csharp
// In Program.cs
builder.Services.AddCloudflareD1Local("app.db");

// In your controller or minimal API
app.MapGet("/users", async (ID1Client d1) =>
{
    var result = await d1.QueryAsync("SELECT * FROM users");
    return Results.Ok(result.Results);
});
```

### With Scoped Service
```csharp
public class UserService
{
    private readonly ID1Client _d1;

    public UserService(ID1Client d1)
    {
        _d1 = d1;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var result = await _d1.QueryAsync("SELECT * FROM users");
        return result.Results.Select(row => new User
        {
            Id = (long)row["id"],
            Name = (string)row["name"],
            Email = (string)row["email"]
        }).ToList();
    }
}
```

## Error Handling

### Basic Try-Catch
```csharp
try
{
    var result = await d1.ExecuteAsync("INSERT INTO users ...");
}
catch (D1Exception ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
```

### With Logging
```csharp
try
{
    var result = await d1.QueryAsync("SELECT * FROM users");
}
catch (D1Exception ex)
{
    _logger.LogError(ex, "Failed to query users");
    throw;
}
```

## Configuration Options

### appsettings.json
```json
{
  "CloudflareD1": {
    "Mode": "Remote",
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token",
    "BaseUrl": "https://api.cloudflare.com/client/v4",
    "Timeout": 30
  }
}
```

### Environment Variables
```bash
CloudflareD1__Mode=Remote
CloudflareD1__AccountId=your-account-id
CloudflareD1__DatabaseId=your-database-id
CloudflareD1__ApiToken=your-api-token
```

## Result Handling

### Accessing Metadata
```csharp
var result = await d1.ExecuteAsync("INSERT INTO users ...");
Console.WriteLine($"Last inserted ID: {result.Meta.LastRowId}");
Console.WriteLine($"Rows changed: {result.Meta.Changes}");
Console.WriteLine($"Duration: {result.Meta.Duration}ms");
```

### Accessing Rows
```csharp
var result = await d1.QueryAsync("SELECT * FROM users");
foreach (var row in result.Results)
{
    var id = (long)row["id"];
    var name = (string)row["name"];
    var email = row.ContainsKey("email") ? (string)row["email"] : null;
}
```

### Checking Success
```csharp
var result = await d1.ExecuteAsync("DELETE FROM users WHERE id = @id", new { id = 999 });
if (result.Meta.Changes == 0)
{
    // Record not found
}
else
{
    // Record deleted
}
```

## Performance Tips

### Use Parameterized Queries
```csharp
// ✅ Good - Prevents SQL injection, better performance
await d1.QueryAsync("SELECT * FROM users WHERE id = @id", new { id });

// ❌ Bad - Security risk, no query plan caching
await d1.QueryAsync($"SELECT * FROM users WHERE id = {id}");
```

### Batch Multiple Operations
```csharp
// ✅ Good - Single round trip
var statements = new[]
{
    new D1Statement("INSERT INTO users ..."),
    new D1Statement("INSERT INTO users ..."),
    new D1Statement("INSERT INTO users ...")
};
await d1.BatchAsync(statements);

// ❌ Bad - Multiple round trips
await d1.ExecuteAsync("INSERT INTO users ...");
await d1.ExecuteAsync("INSERT INTO users ...");
await d1.ExecuteAsync("INSERT INTO users ...");
```

### Use Local Mode for Development
```csharp
// In Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCloudflareD1Local("dev.db");
}
else
{
    builder.Services.AddCloudflareD1(
        builder.Configuration.GetSection("CloudflareD1")
    );
}
```

## Testing

### Mock ID1Client
```csharp
public class UserServiceTests
{
    [Fact]
    public async Task GetUsers_ReturnsUsers()
    {
        // Arrange
        var mockD1 = new Mock<ID1Client>();
        mockD1.Setup(x => x.QueryAsync(It.IsAny<string>(), null, null))
              .ReturnsAsync(new D1QueryResult
              {
                  Results = new List<Dictionary<string, object>>
                  {
                      new() { ["id"] = 1L, ["name"] = "Test" }
                  }
              });

        var service = new UserService(mockD1.Object);

        // Act
        var users = await service.GetUsersAsync();

        // Assert
        Assert.Single(users);
        Assert.Equal("Test", users[0].Name);
    }
}
```

## Common SQL Patterns

### Pagination
```csharp
var result = await d1.QueryAsync(
    "SELECT * FROM users ORDER BY id LIMIT @limit OFFSET @offset",
    new { limit = 10, offset = page * 10 }
);
```

### Search
```csharp
var result = await d1.QueryAsync(
    "SELECT * FROM users WHERE name LIKE @search OR email LIKE @search",
    new { search = $"%{searchTerm}%" }
);
```

### Aggregation
```csharp
var result = await d1.QueryAsync(@"
    SELECT 
        status,
        COUNT(*) as count,
        AVG(amount) as avg_amount
    FROM orders
    GROUP BY status
");
```

### Join
```csharp
var result = await d1.QueryAsync(@"
    SELECT u.name, o.total
    FROM users u
    INNER JOIN orders o ON u.id = o.user_id
    WHERE o.status = @status
", new { status = "completed" });
```

## Migration Example

```csharp
public class DatabaseMigrator
{
    private readonly ID1Client _d1;

    public async Task MigrateAsync()
    {
        // Create migrations table
        await _d1.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                version INTEGER NOT NULL UNIQUE,
                applied_at TEXT DEFAULT CURRENT_TIMESTAMP
            )
        ");

        // Check current version
        var result = await _d1.QueryAsync(
            "SELECT MAX(version) as version FROM migrations"
        );
        var currentVersion = result.Results[0]["version"] as long? ?? 0;

        // Apply migrations
        if (currentVersion < 1)
        {
            await ApplyMigration1();
        }
        if (currentVersion < 2)
        {
            await ApplyMigration2();
        }
    }

    private async Task ApplyMigration1()
    {
        var statements = new[]
        {
            new D1Statement("CREATE TABLE users (...)"),
            new D1Statement("INSERT INTO migrations (version) VALUES (1)")
        };
        await _d1.BatchAsync(statements);
    }
}
```

---

## Resources

- 📖 [Full Documentation](https://jdtoon.github.io/CloudflareD1.NET/)
- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)
- 💻 [Sample Applications](https://github.com/jdtoon/CloudflareD1.NET/tree/main/samples)
- 🐛 [Report Issues](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 💬 [GitHub Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)
