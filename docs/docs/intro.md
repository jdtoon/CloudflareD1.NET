---
sidebar_position: 1
---

# Introduction

Welcome to **CloudflareD1.NET** - a complete .NET adapter for Cloudflare D1, the serverless SQL database that runs on Cloudflare's edge network.

## What is CloudflareD1.NET?

CloudflareD1.NET is a comprehensive, production-ready library that provides seamless integration between your .NET applications and Cloudflare's D1 database. It bridges the gap between the .NET ecosystem and Cloudflare's innovative edge database platform.

## Why CloudflareD1.NET?

### 🚀 Dual Mode Support

- **Local Development**: Use local SQLite for development without any Cloudflare setup
- **Production Ready**: Seamlessly switch to Cloudflare D1 for production deployments
- **Zero Configuration**: Works out of the box with sensible defaults

### 💪 Feature Complete

- Full implementation of Cloudflare D1 REST API
- Batch operations and transactions
- Time Travel queries (query historical data)
- Database management (create, list, delete)
- Parameterized queries with SQL injection protection

### 🛠️ Developer Friendly

- Strong typing with full XML documentation
- ASP.NET Core dependency injection support
- Comprehensive logging with ILogger integration
- Async/await patterns throughout
- Extensive examples and documentation

## Key Features

- ✅ **Query Execution**: Execute SELECT, INSERT, UPDATE, DELETE queries
- ✅ **Batch Operations**: Run multiple queries as atomic transactions
- ✅ **Parameterized Queries**: Safe SQL execution with named or positional parameters
- ✅ **Time Travel**: Query data at any point in time (Cloudflare D1 feature)
- ✅ **Database Management**: Create, list, and delete D1 databases programmatically
- ✅ **Local & Remote**: Develop locally with SQLite, deploy to Cloudflare D1
- ✅ **DI Integration**: First-class ASP.NET Core dependency injection support
- ✅ **Flexible Auth**: Support for API Token and API Key authentication

## Quick Example

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Options;

// Configure for local development
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "myapp.db"
});

// Create client
using var client = new D1Client(options, logger);

// Execute queries
await client.ExecuteAsync(@"
    CREATE TABLE users (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        email TEXT UNIQUE
    )
");

await client.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    new { name = "John Doe", email = "john@example.com" }
);

var result = await client.QueryAsync("SELECT * FROM users");
foreach (var user in result.Results)
{
    Console.WriteLine($"{user["name"]}: {user["email"]}");
}
```

## Target Frameworks

CloudflareD1.NET targets **.NET Standard 2.1**, providing broad compatibility:

- ✅ .NET Core 3.0+
- ✅ .NET 5, 6, 7, 8+
- ✅ ASP.NET Core 3.0+
- ✅ Blazor Server & WebAssembly (with server-side API)

## Next Steps

Ready to get started? Check out these resources:

- [Installation Guide](getting-started/installation) - Install the NuGet package
- [Quick Start](getting-started/quick-start) - Get up and running in minutes
- [Configuration](getting-started/configuration) - Learn about all configuration options
- [API Reference](api/d1client) - Detailed API documentation

## Support & Community

- 📖 [Full Documentation](https://yourusername.github.io/CloudflareD1.NET/)
- 💬 [GitHub Discussions](https://github.com/yourusername/CloudflareD1.NET/discussions)
- 🐛 [Report Issues](https://github.com/yourusername/CloudflareD1.NET/issues)
- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)

## License

CloudflareD1.NET is [MIT licensed](https://github.com/yourusername/CloudflareD1.NET/blob/main/LICENSE).
