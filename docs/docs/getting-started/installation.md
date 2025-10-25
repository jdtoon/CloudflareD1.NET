---
sidebar_position: 1
---

# Installation

Install CloudflareD1.NET via NuGet Package Manager.

## Requirements

- **.NET Standard 2.1** or higher
- Works with **.NET Core 3.0+**, **.NET 5+**, **.NET 6+**, **.NET 8+**
- For local mode: No additional requirements
- For remote mode: Cloudflare account with D1 database

## Install via .NET CLI

```bash
dotnet add package CloudflareD1.NET
```

## Install via Package Manager Console

```powershell
Install-Package CloudflareD1.NET
```

## Install via NuGet Package Manager UI

1. Right-click on your project in Visual Studio
2. Select "Manage NuGet Packages"
3. Search for "CloudflareD1.NET"
4. Click "Install"

## Verify Installation

After installation, verify that the package is referenced in your project file:

```xml title="YourProject.csproj"
<ItemGroup>
  <PackageReference Include="CloudflareD1.NET" Version="1.0.0" />
</ItemGroup>
```

## Optional Packages

For ASP.NET Core applications with full DI support, you may also want:

```bash
dotnet add package Microsoft.Extensions.Logging.Console
dotnet add package Microsoft.Extensions.Configuration.Json
```

## Next Steps

Now that you have CloudflareD1.NET installed, proceed to:

- [Quick Start Guide](quick-start) - Build your first application
- [Configuration](configuration) - Learn about configuration options
