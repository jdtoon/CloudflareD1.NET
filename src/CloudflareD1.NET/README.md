# CloudflareD1.NET

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.svg)](https://www.nuget.org/packages/CloudflareD1.NET/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A complete .NET adapter for **Cloudflare D1** - the serverless SQL database running on Cloudflare's edge network. This library provides seamless integration with Cloudflare's D1 database, supporting both **local SQLite development** and **remote D1 production deployments**.

## Features

- ✅ **Dual Mode Support**: Seamlessly switch between local SQLite (development) and remote D1 (production)
- ✅ **Full CRUD Operations**: Execute queries, inserts, updates, deletes, and batch operations
- ✅ **Parameterized Queries**: Prevent SQL injection with built-in parameter binding
- ✅ **Time Travel Queries**: Query historical data with D1's time travel capabilities
- ✅ **Database Management**: Create, list, and query database metadata
- ✅ **Type-Safe**: Strongly typed models and response objects
- ✅ **Async/Await**: Full async support throughout the library
- ✅ **Comprehensive Error Handling**: Detailed exception types for different error scenarios
- ✅ **Well Tested**: 183 unit tests ensuring reliability

## Installation

```bash
dotnet add package CloudflareD1.NET
```

## Quick Start

```csharp
using CloudflareD1.NET;

// Configure for remote D1 (production)
var config = new D1Configuration
{
    AccountId = "your-account-id",
    DatabaseId = "your-database-id",
    ApiToken = "your-api-token"
};

var client = new D1Client(config);

// Execute a query
var result = await client.QueryAsync<User>("SELECT * FROM users WHERE id = ?", 1);

// Insert data
await client.ExecuteAsync(
    "INSERT INTO users (name, email) VALUES (?, ?)",
    "John Doe",
    "john@example.com"
);
```

## Related Packages

- **[CloudflareD1.NET.Linq](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)** - LINQ query builder with IQueryable support, compiled queries, async streaming
- **[CloudflareD1.NET.Migrations](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)** - Database migration system with version tracking
- **[dotnet-d1](https://www.nuget.org/packages/dotnet-d1/)** - CLI tool for managing migrations

## Documentation

Full documentation is available at [https://jdtoon.github.io/CloudflareD1.NET](https://jdtoon.github.io/CloudflareD1.NET)

## License

MIT License - see [LICENSE](https://github.com/jdtoon/CloudflareD1.NET/blob/master/LICENSE) for details
