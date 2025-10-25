---
sidebar_position: 2
---

# Quick Start

Get up and running with CloudflareD1.NET in just a few minutes.

## Local Development Mode

For local development, CloudflareD1.NET uses SQLite - no Cloudflare account needed!

### 1. Create a Console Application

```bash
dotnet new console -n MyD1App
cd MyD1App
dotnet add package CloudflareD1.NET
dotnet add package Microsoft.Extensions.Logging.Console
```

### 2. Write Your First Query

```csharp title="Program.cs"
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole().SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger<D1Client>();

// Configure D1 for local mode
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "myapp.db"
});

// Create client
using var client = new D1Client(options, logger);

// Create a table
await client.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS tasks (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        title TEXT NOT NULL,
        completed INTEGER DEFAULT 0
    )
");

// Insert data
await client.ExecuteAsync(
    "INSERT INTO tasks (title) VALUES (@title)",
    new { title = "Learn CloudflareD1.NET" }
);

// Query data
var result = await client.QueryAsync("SELECT * FROM tasks");
foreach (var task in result.Results)
{
    Console.WriteLine($"Task: {task["title"]} (ID: {task["id"]})");
}
```

### 3. Run Your App

```bash
dotnet run
```

You should see output like:
```
Task: Learn CloudflareD1.NET (ID: 1)
```

## ASP.NET Core Application

### 1. Create Web API

```bash
dotnet new webapi -n MyD1Api
cd MyD1Api
dotnet add package CloudflareD1.NET
```

### 2. Configure Services

```csharp title="Program.cs"
using CloudflareD1.NET.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add CloudflareD1.NET
builder.Services.AddCloudflareD1Local("myapp.db");

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

### 3. Create a Controller

```csharp title="Controllers/TasksController.cs"
using CloudflareD1.NET;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ID1Client _d1;

    public TasksController(ID1Client d1)
    {
        _d1 = d1;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var result = await _d1.QueryAsync("SELECT * FROM tasks");
        return Ok(result.Results);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var result = await _d1.ExecuteAsync(
            "INSERT INTO tasks (title) VALUES (@title)",
            new { title = request.Title }
        );
        
        return CreatedAtAction(
            nameof(GetTasks), 
            new { id = result.Meta?.LastRowId }
        );
    }
}

public record CreateTaskRequest(string Title);
```

### 4. Initialize Database

Add this to ensure the table exists when your app starts:

```csharp title="Program.cs"
// After app.Build()
using (var scope = app.Services.CreateScope())
{
    var d1 = scope.ServiceProvider.GetRequiredService<ID1Client>();
    await d1.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS tasks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            completed INTEGER DEFAULT 0
        )
    ");
}

app.Run();
```

### 5. Test Your API

```bash
# Run the app
dotnet run

# In another terminal, test the endpoints
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Build something awesome"}'

curl http://localhost:5000/api/tasks
```

## Switch to Production (Cloudflare D1)

When you're ready for production, simply update your configuration:

```csharp title="appsettings.Production.json"
{
  "CloudflareD1": {
    "UseLocalMode": false,
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token"
  }
}
```

```csharp title="Program.cs"
builder.Services.AddCloudflareD1(
    builder.Configuration.GetSection("CloudflareD1")
);
```

**That's it!** Your code stays the same, just the configuration changes.

## Next Steps

Check out the [sample applications](https://github.com/jdtoon/CloudflareD1.NET/tree/main/samples) on GitHub for more complete examples.
