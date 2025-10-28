---
sidebar_position: 1
---

# Introduction

Welcome to **CloudflareD1.NET** - a complete .NET adapter for Cloudflare D1, the serverless SQL database that runs on Cloudflare's edge network.

:::tip What's New in v1.2.1
**Computed Properties in Select()** are now available! Generate new values dynamically:
```csharp
var users = await client.Query<User>("users")
    .Select(u => new {
        u.Id,
        u.Name,
        u.Age,
        IsAdult = u.Age >= 18,
        Total = u.Price * u.Quantity
    })
    .ToListAsync();
```
Learn more in the [Expression Trees](/docs/linq/expression-trees#computed-properties-in-select-v121) documentation.
:::

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

    - JSON snapshot: `Migrations/.migrations-snapshot.json`
    - Incremental diffs vs last snapshot (not the DB)
    - DROP COLUMN supported via table recreation pattern
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

## Support & Community

- 📖 [Full Documentation](https://jdtoon.github.io/CloudflareD1.NET/)
- 💬 [GitHub Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)
- 🐛 [Report Issues](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)

## License

CloudflareD1.NET is [MIT licensed](https://github.com/jdtoon/CloudflareD1.NET/blob/main/LICENSE).

