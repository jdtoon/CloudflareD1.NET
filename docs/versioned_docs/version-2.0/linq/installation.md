---
sidebar_position: 2
---

# Installation

Learn how to install and configure the CloudflareD1.NET.Linq package in your project.

## Prerequisites

- .NET Standard 2.1 or higher (.NET Core 3.0+, .NET 5+)
- CloudflareD1.NET package (automatically installed as dependency)

## Install via NuGet

### Package Manager Console

```powershell
Install-Package CloudflareD1.NET.Linq
```

### .NET CLI

```bash
dotnet add package CloudflareD1.NET.Linq
```

### PackageReference

Add to your `.csproj` file:

```xml
<PackageReference Include="CloudflareD1.NET.Linq" Version="1.0.0" />
```

## Verify Installation

After installation, verify that both packages are installed:

```bash
dotnet list package
```

You should see:
```
CloudflareD1.NET              1.0.1
CloudflareD1.NET.Linq         1.0.0
```

## Setup in Your Code

### Add Using Statement

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.Linq;
```

### No Additional Configuration Required

The LINQ extensions automatically work with your existing `ID1Client` instance:

```csharp
// Your existing D1Client setup
var options = Options.Create(new D1Options
{
    UseLocalMode = false,
    AccountId = "your-account-id",
    DatabaseId = "your-database-id",
    ApiToken = "your-api-token"
});

var client = new D1Client(options, logger);

// LINQ extensions now available!
var users = await client.QueryAsync<User>("SELECT * FROM users");
```

## Dependency Injection (ASP.NET Core)

If you're using dependency injection with the core package, the LINQ extensions work automatically:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddCloudflareD1(options =>
{
    options.AccountId = builder.Configuration["Cloudflare:AccountId"];
    options.DatabaseId = builder.Configuration["Cloudflare:DatabaseId"];
    options.ApiToken = builder.Configuration["Cloudflare:ApiToken"];
});

// In your controller or service
public class UserService
{
    private readonly ID1Client _client;

    public UserService(ID1Client client)
    {
        _client = client;
    }

    public async Task<List<User>> GetActiveUsers()
    {
        // LINQ methods available on injected client
        return (await _client.Query<User>("users")
            .Where("is_active = ?", true)
            .ToListAsync())
            .ToList();
    }
}
```

## What's Next?

Now that you have the package installed, learn how to use it:

- **[Query Builder](query-builder)** - Start building fluent queries
- **[Entity Mapping](entity-mapping)** - Define your entity classes
