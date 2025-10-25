# CloudflareD1.NET Samples

This directory contains sample applications demonstrating different usage scenarios for CloudflareD1.NET.

## Available Samples

### 1. ConsoleApp.Sample
A complete console application demonstrating all core features:
- Local SQLite mode configuration
- Database initialization and table creation
- CRUD operations (Create, Read, Update, Delete)
- Parameterized queries for security
- Batch operations with transactions
- Query execution and result handling

**Best for**: Getting started, understanding basic concepts, testing library features

[View Sample →](./ConsoleApp.Sample/)

### 2. WebApi.Sample
A minimal Web API built with ASP.NET Core demonstrating:
- REST API design with CloudflareD1.NET
- Dependency injection setup
- CRUD endpoints for todo management
- Statistics and reporting endpoints
- HTTP status code handling
- JSON serialization of D1 results

**Best for**: Building production APIs, microservices, backend services

[View Sample →](./WebApi.Sample/)

## Running the Samples

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider (optional)

### Console App
```bash
cd samples/ConsoleApp.Sample
dotnet run
```

### Web API
```bash
cd samples/WebApi.Sample
dotnet run
```

Then test with:
```bash
# Get all todos
curl http://localhost:5147/todos

# Create a todo
curl -X POST http://localhost:5147/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"Test item","description":"Testing the API"}'

# Get statistics
curl http://localhost:5147/todos/stats
```

## Sample Structure

Each sample follows a consistent structure:
```
SampleName/
├── Program.cs              # Main application code
├── SampleName.csproj       # Project file with dependencies
└── README.md               # Sample-specific documentation
```

## Configuration Examples

### Local Mode (Development)
```csharp
builder.Services.AddCloudflareD1Local("myapp.db");
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
builder.Services.AddCloudflareD1(builder.Configuration.GetSection("CloudflareD1"));
```

With `appsettings.json`:
```json
{
  "CloudflareD1": {
    "Mode": "Remote",
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token"
  }
}
```

## Common Patterns

### Database Initialization
```csharp
using (var scope = app.Services.CreateScope())
{
    var d1 = scope.ServiceProvider.GetRequiredService<ID1Client>();
    await d1.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT UNIQUE
        )
    ");
}
```

### Parameterized Queries
```csharp
var result = await d1.QueryAsync(
    "SELECT * FROM users WHERE email = @email",
    new { email = userEmail }
);
```

### Batch Operations
```csharp
var statements = new[]
{
    new D1Statement("INSERT INTO users (name, email) VALUES ('John', 'john@example.com')"),
    new D1Statement("INSERT INTO users (name, email) VALUES ('Jane', 'jane@example.com')"),
    new D1Statement("UPDATE settings SET last_sync = @time", new { time = DateTime.UtcNow })
};

var results = await d1.BatchAsync(statements);
```

### Error Handling
```csharp
try
{
    var result = await d1.ExecuteAsync("INSERT INTO users ...");
}
catch (D1Exception ex)
{
    // Handle D1-specific errors
    logger.LogError(ex, "Database operation failed: {Message}", ex.Message);
}
```

## Additional Resources

- 📖 [Full Documentation](https://jdtoon.github.io/CloudflareD1.NET/)
- 🏠 [Main Repository](https://github.com/jdtoon/CloudflareD1.NET)
- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)
- 💬 [Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)
- 🐛 [Report Issues](https://github.com/jdtoon/CloudflareD1.NET/issues)

## Contributing

Want to add a new sample? We welcome contributions!

1. Create a new folder under `samples/`
2. Follow the existing sample structure
3. Include a comprehensive README.md
4. Add to this index file
5. Submit a pull request

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## License

All samples are provided under the MIT License. See [LICENSE](../LICENSE) for details.
