# CloudflareD1.NET

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.svg)](https://www.nuget.org/packages/CloudflareD1.NET/)
[![NuGet - Linq](https://img.shields.io/nuget/v/CloudflareD1.NET.Linq.svg?label=Linq)](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)
[![NuGet - CodeFirst](https://img.shields.io/nuget/v/CloudflareD1.NET.CodeFirst.svg?label=CodeFirst)](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)
[![NuGet - Migrations](https://img.shields.io/nuget/v/CloudflareD1.NET.Migrations.svg?label=Migrations)](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)
[![NuGet - CLI](https://img.shields.io/nuget/v/dotnet-d1.svg?label=CLI%20Tool)](https://www.nuget.org/packages/dotnet-d1/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A complete .NET adapter for **Cloudflare D1** - the serverless SQL database running on Cloudflare's edge network. This library provides seamless integration with Cloudflare's D1 database, supporting both **local SQLite development** and **remote D1 production deployments**.

## 📦 Packages

| Package | Description | Install |
|---------|-------------|---------|
| **CloudflareD1.NET** | Core package with raw SQL support | `dotnet add package CloudflareD1.NET` |
| **CloudflareD1.NET.Linq** | LINQ queries and object mapping | `dotnet add package CloudflareD1.NET.Linq` |
| **CloudflareD1.NET.CodeFirst** | Code-First ORM with DbContext pattern | `dotnet add package CloudflareD1.NET.CodeFirst` |
| **CloudflareD1.NET.Migrations** | Database migrations and schema management | `dotnet add package CloudflareD1.NET.Migrations` |
| **dotnet-d1** | CLI tool for managing migrations | `dotnet tool install -g dotnet-d1` |

## ✨ Features

### Core Package (CloudflareD1.NET)
- 🚀 **Full D1 API Support** - Complete implementation of Cloudflare D1 REST API
- 🏠 **Local Development Mode** - Use local SQLite for development without any setup
- 🔄 **Seamless Switching** - Easy toggle between local and remote modes
- 📦 **Batch Operations** - Execute multiple queries as a single transaction
- ⏱️ **Time Travel Queries** - Query historical data (D1 feature)
- 🛠️ **Database Management** - Create, list, and delete D1 databases programmatically
- 💉 **Dependency Injection** - Full ASP.NET Core DI integration
- 🔐 **Flexible Authentication** - Support for API Token and API Key authentication
- 📝 **Comprehensive Logging** - Built-in logging with ILogger support
- 🎯 **Type-Safe** - Strongly typed with full XML documentation
- ⚡ **Async/Await** - Modern async patterns throughout
- 🧪 **Well Tested** - Comprehensive test coverage

### LINQ Package (CloudflareD1.NET.Linq)
- ✨ **IQueryable&lt;T&gt; Support** - Standard LINQ with deferred execution (v1.3.0+)
- 🔗 **Join Operations** - INNER JOIN and LEFT JOIN support (v1.6.0)
- 📊 **GroupBy & Having** - Group results with aggregate filters (v1.5.0+)
- 🎯 **Select() Projections** - Project to DTOs with computed properties (v1.4.0)
- 🔍 **Distinct()** - Remove duplicate rows (v1.7.0)
- 📋 **Contains()/IN Clause** - Collection filtering support (v1.7.0)
- 🔀 **Set Operations** - Union, UnionAll, Intersect, Except (NEW in v1.8.0-beta)
- 🎯 **Type-Safe Queries** - `QueryAsync<T>()` with automatic entity mapping
- 🗺️ **Automatic Mapping** - Snake_case columns to PascalCase properties  
- 💪 **Strongly Typed** - Compile-time type checking for queries
- ⚡ **High Performance** - Reflection caching for minimal overhead
- 🎨 **Custom Mappers** - Implement `IEntityMapper` for custom logic
- 📋 **LINQ Methods** - QueryFirstOrDefaultAsync, QuerySingleAsync, etc.

### CodeFirst Package (CloudflareD1.NET.CodeFirst)
- 🎯 **DbContext Pattern** - Familiar API similar to Entity Framework Core
- 🏗️ **Entity Attributes** - Define tables, columns, keys using attributes
- 🔗 **Relationships** - One-to-Many, Many-to-One navigation properties
- 🔑 **Key Management** - Primary keys, foreign keys, composite keys
- ✅ **Validation** - Required fields, data annotations
- 🎨 **Fluent API** - Configure entities programmatically
- 🔄 **Type-Safe Queries** - LINQ integration through D1Set&lt;T&gt;
- 📦 **Model-Driven** - Database schema from C# classes
- 🛠️ **Migration Generation** - Generate migrations from model changes

### Migrations Package (CloudflareD1.NET.Migrations)
- 🔄 **Version Control** - Track database schema changes over time
- 📝 **Fluent API** - Intuitive builder for creating tables and indexes
- ⬆️ **Up/Down Migrations** - Forward and rollback support
- 📜 **Migration History** - Automatic tracking of applied migrations
- 🛠️ **CLI Tool** - dotnet-d1 command-line tool for migration management
- 🎯 **Type-Safe** - Strongly typed schema definitions
- 🔧 **Schema Operations** - CREATE/DROP tables, ADD/DROP columns, indexes
- 🔑 **Constraints** - Primary keys, foreign keys, unique, check constraints
- 📦 **Programmatic API** - Apply migrations from code
- ✅ **Well Tested** - Comprehensive unit test coverage

## 📦 Installation

### Core Package
```bash
dotnet add package CloudflareD1.NET
```

### With LINQ Support  
```bash
dotnet add package CloudflareD1.NET.Linq
```

### With Code-First ORM
```bash
dotnet add package CloudflareD1.NET.CodeFirst
```

### With Migrations Support
```bash
# Install the migrations package
dotnet add package CloudflareD1.NET.Migrations

# Install the CLI tool globally
dotnet tool install -g dotnet-d1
```

Or via Package Manager Console:

```powershell
Install-Package CloudflareD1.NET
```

## 🚀 Quick Start

### Local Development Mode (Default)

Perfect for development and testing without needing Cloudflare credentials:

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

// Configure for local SQLite mode
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "myapp.db"
});

var logger = loggerFactory.CreateLogger<D1Client>();

// Create client
using var client = new D1Client(options, logger);

// Create table
await client.ExecuteAsync(@"
    CREATE TABLE users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT UNIQUE NOT NULL
    )
");

// Insert data
await client.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "John Doe", email = "john@example.com" }
);

// Query data
var result = await client.QueryAsync("SELECT * FROM users");
foreach (var row in result.Results)
{
    Console.WriteLine($"{row["name"]}: {row["email"]}");
}
```

### Remote Cloudflare D1 Mode

For production with actual Cloudflare D1 databases:

```csharp
var options = Options.Create(new D1Options
{
    UseLocalMode = false,
    AccountId = "your-cloudflare-account-id",
    DatabaseId = "your-d1-database-id",
    ApiToken = "your-cloudflare-api-token"
});

using var client = new D1Client(options, logger);

// Same API as local mode!
var result = await client.QueryAsync("SELECT * FROM users WHERE name = @name", 
    new { name = "John" });
```

## 🔧 ASP.NET Core Integration

### With Configuration

In `appsettings.json`:

```json
{
  "CloudflareD1": {
    "UseLocalMode": true,
    "LocalDatabasePath": "myapp.db"
  }
}
```

In `Program.cs` or `Startup.cs`:

```csharp
using CloudflareD1.NET.Extensions;

// Add D1 services
builder.Services.AddCloudflareD1(builder.Configuration.GetSection("CloudflareD1"));

// Or configure directly
builder.Services.AddCloudflareD1(options =>
{
    options.UseLocalMode = true;
    options.LocalDatabasePath = "myapp.db";
});
```

### Using in Controllers

```csharp
using CloudflareD1.NET;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ID1Client _d1Client;

    public UsersController(ID1Client d1Client)
    {
        _d1Client = d1Client;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _d1Client.QueryAsync("SELECT * FROM users");
        return Ok(result.Results);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        var result = await _d1Client.ExecuteAsync(
            "INSERT INTO users (name, email) VALUES (@name, @email)",
            user
        );
        
        return CreatedAtAction(nameof(GetUsers), new { id = result.Meta?.LastRowId });
    }
}
```

## 📚 Advanced Usage

### Batch Operations (Transactions)

Execute multiple statements as a single atomic transaction:

```csharp
using CloudflareD1.NET.Models;

var statements = new List<D1Statement>
{
    new() { 
        Sql = "INSERT INTO users (name, email) VALUES (@name, @email)", 
        Params = new { name = "Alice", email = "alice@example.com" } 
    },
    new() { 
        Sql = "INSERT INTO orders (user_id, total) VALUES (@userId, @total)", 
        Params = new { userId = 1, total = 99.99 } 
    },
    new() { 
        Sql = "UPDATE stats SET user_count = user_count + 1" 
    }
};

var results = await client.BatchAsync(statements);
// All succeed or all fail together
```

### Time Travel Queries (Cloudflare D1 Only)

Query your database at a specific point in time:

```csharp
using CloudflareD1.NET;

var managementClient = (ID1ManagementClient)client;

// Query data as it was 24 hours ago
var timestamp = DateTime.UtcNow.AddDays(-1).ToString("o");
var historicalData = await managementClient.QueryAtTimestampAsync(
    "SELECT * FROM users",
    timestamp
);
```

### Database Management

```csharp
using CloudflareD1.NET;

var managementClient = (ID1ManagementClient)client;

// List all databases
var databases = await managementClient.ListDatabasesAsync();

// Create a new database
var newDb = await managementClient.CreateDatabaseAsync("my-new-database");

// Get database info
var dbInfo = await managementClient.GetDatabaseAsync("database-id");

// Delete a database
await managementClient.DeleteDatabaseAsync("database-id");
```

## 🎯 Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UseLocalMode` | bool | `true` | Use local SQLite instead of Cloudflare D1 |
| `LocalDatabasePath` | string | `"local.db"` | Path to local SQLite database file |
| `AccountId` | string | `null` | Cloudflare Account ID (required for remote) |
| `DatabaseId` | string | `null` | D1 Database ID (required for remote) |
| `ApiToken` | string | `null` | Cloudflare API Token for authentication |
| `ApiKey` | string | `null` | Cloudflare API Key (legacy auth) |
| `Email` | string | `null` | Email for API Key authentication |
| `ApiBaseUrl` | string | `https://api.cloudflare.com/client/v4` | Cloudflare API base URL |
| `TimeoutSeconds` | int | `30` | HTTP request timeout |

## 🔐 Authentication

### API Token (Recommended)

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/profile/api-tokens)
2. Create API Token with **D1 Edit** permissions
3. Use in configuration:

```csharp
options.ApiToken = "your-api-token";
```

### API Key (Legacy)

```csharp
options.ApiKey = "your-api-key";
options.Email = "your-email@example.com";
```

## 📖 Documentation

Full documentation is available at [https://your-docs-site.com](https://your-docs-site.com) (coming soon)

## 🧪 Testing

The library includes a comprehensive test suite. Run tests with:

```bash
dotnet test
```

## � Database Migrations

CloudflareD1.NET.Migrations provides a complete database migration system for managing schema changes over time.

### Quick Start with Migrations

```bash
# Install the CLI tool
dotnet tool install -g dotnet-d1

# Create a new migration
dotnet d1 migrations add CreateUsersTable

# Apply migrations
dotnet d1 database update

# Rollback last migration
dotnet d1 database rollback
```

### Creating Migrations

Migrations use a fluent API for defining schema changes:

```csharp
using CloudflareD1.NET.Migrations;

public class CreateUsersTable_20241027120000 : Migration
{
    public override string Id => "20241027120000";
    public override string Name => "CreateUsersTable";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey().AutoIncrement();
            t.Text("name").NotNull();
            t.Text("email").NotNull().Unique();
            t.Integer("age");
            t.Text("created_at").Default("CURRENT_TIMESTAMP");
        });

        builder.CreateIndex("idx_users_email", "users", new[] { "email" }, unique: true);
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropIndex("idx_users_email");
        builder.DropTable("users");
    }
}
```

### Programmatic Usage

```csharp
using CloudflareD1.NET.Migrations;

// Get migrations from assembly
var migrations = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
    .Select(t => (Migration)Activator.CreateInstance(t)!)
    .ToList();

// Create migration runner
var runner = new MigrationRunner(client, migrations);

// Apply all pending migrations
var applied = await runner.MigrateAsync();

// Rollback last migration
var rolledBack = await runner.RollbackAsync();
```

### Features

- ✅ **Fluent API** - Intuitive builder for schema changes
- ✅ **Up/Down Migrations** - Full rollback support
- ✅ **Migration History** - Automatic tracking in `__migrations` table
- ✅ **CLI Tool** - `dotnet-d1` for managing migrations
- ✅ **Type-Safe** - Strongly typed schema definitions
- ✅ **Schema Operations** - CREATE/DROP tables, ADD/DROP columns, indexes
- ✅ **Constraints** - Primary keys, foreign keys, unique, check constraints

For complete documentation, see the [Migrations Guide](https://cloudflareb1-net-docs.pages.dev/docs/migrations/overview).

## �🔮 Future Enhancements

The following features are planned for future releases, pending Cloudflare D1 REST API support:

### Transactions (Pending Cloudflare API)
- **ITransaction Interface** - Explicit transaction control with Begin/Commit/Rollback
- **Atomic Operations** - Multiple operations within a single transaction
- **Auto-rollback** - Automatic rollback on dispose if not committed

**Note:** Cloudflare D1 currently only supports transactions in the Workers environment. Once transaction support is added to the REST API, we will implement these features.

### Batch Operations (Pending Cloudflare API)
- **BatchInsertAsync** - Bulk insert multiple entities efficiently
- **BatchUpdateAsync** - Bulk update with automatic property mapping
- **BatchDeleteAsync** - Bulk delete operations
- **UpsertAsync** - Insert or update based on existence

**Note:** While basic batch execution is supported via `BatchAsync()`, advanced batch operations with automatic entity mapping require transaction support from Cloudflare's REST API.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Cloudflare for the amazing D1 database platform
- The .NET community for excellent tooling and support

## 📮 Support

- 📫 Open an issue on [GitHub Issues](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 💬 Start a discussion on [GitHub Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)

## 🔗 Links

- [Cloudflare D1 Documentation](https://developers.cloudflare.com/d1/)
- [Cloudflare D1 API Reference](https://developers.cloudflare.com/api/resources/d1/)
- [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)

---

Made with ❤️ by the .NET and Cloudflare communities

