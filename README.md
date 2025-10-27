# CloudflareD1.NET

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.svg)](https://www.nuget.org/packages/CloudflareD1.NET/)
[![NuGet - Linq](https://img.shields.io/nuget/v/CloudflareD1.NET.Linq.svg?label=Linq)](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A complete .NET adapter for **Cloudflare D1** - the serverless SQL database running on Cloudflare's edge network. This library provides seamless integration with Cloudflare's D1 database, supporting both **local SQLite development** and **remote D1 production deployments**.

## 📦 Packages

| Package | Description | Install |
|---------|-------------|---------|
| **CloudflareD1.NET** | Core package with raw SQL support | `dotnet add package CloudflareD1.NET` |
| **CloudflareD1.NET.Linq** | LINQ queries and object mapping | `dotnet add package CloudflareD1.NET.Linq` |

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
- ✨ **IQueryable<T> Support** - Standard LINQ with deferred execution (v1.3.0+)
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

## 📦 Installation

### Core Package
```bash
dotnet add package CloudflareD1.NET
```

### With LINQ Support  
```bash
dotnet add package CloudflareD1.NET.Linq
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

