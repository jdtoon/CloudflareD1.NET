# dotnet-d1 CLI Tool

[![NuGet](https://img.shields.io/nuget/v/dotnet-d1.svg)](https://www.nuget.org/packages/dotnet-d1/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Command-line tool for managing Cloudflare D1 database migrations.

## Installation

Install globally as a .NET tool:

```bash
dotnet tool install -g dotnet-d1
```

Update to latest version:

```bash
dotnet tool update -g dotnet-d1
```

## Usage

### Create a New Migration

Generate a timestamped migration file:

```bash
dotnet d1 migrations add CreateUsersTable
```

This creates a new file: `Migrations/YYYYMMDDHHMMSS_CreateUsersTable.cs`

```csharp
using CloudflareD1.NET.Migrations;

public class CreateUsersTable : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        // Create schema changes
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey().AutoIncrement();
            t.Text("name").NotNull();
            t.Text("email").NotNull().Unique();
        });
    }

    public override void Down(MigrationBuilder builder)
    {
        // Reverse schema changes
        builder.DropTable("users");
    }
}
```

### Apply Pending Migrations

Apply all migrations that haven't been run yet:

```bash
dotnet d1 migrations update
```

Options:
- Reads `appsettings.json` for D1 connection details
- Tracks applied migrations in `__migrations` table
- Runs migrations in timestamp order

### List All Migrations

Show migration status (applied vs pending):

```bash
dotnet d1 migrations list
```

Output example:
```
Applied Migrations:
  ✓ 20251027091358_CreateUsersTable
  ✓ 20251027091400_CreatePostsTable

Pending Migrations:
  ○ 20251027091500_AddUserPhoneColumn
```

### Rollback Last Migration

Undo the most recently applied migration:

```bash
dotnet d1 migrations rollback
```

This executes the `Down()` method of the last migration and removes it from history.

## Configuration

Create `appsettings.json` in your project root:

```json
{
  "D1Configuration": {
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token"
  }
}
```

**Security Note**: Add `appsettings.json` to `.gitignore` to protect credentials. Use `appsettings.example.json` for templates.

## Migration Best Practices

1. **Always provide Down() methods** for rollback support
2. **Test migrations** against a dev database first
3. **Use transactions** when possible (multiple related changes)
4. **Keep migrations small** and focused on single changes
5. **Never modify applied migrations** - create new ones instead
6. **Backup production data** before running migrations

## Requirements

- .NET 8.0 or later
- CloudflareD1.NET.Migrations package in your project

## Related Packages

- **[CloudflareD1.NET](https://www.nuget.org/packages/CloudflareD1.NET/)** - Core database client
- **[CloudflareD1.NET.Linq](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)** - LINQ query builder
- **[CloudflareD1.NET.Migrations](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)** - Migration library

## Documentation

Full documentation is available at [https://jdtoon.github.io/CloudflareD1.NET/docs/migrations/overview](https://jdtoon.github.io/CloudflareD1.NET/docs/migrations/overview)

## License

MIT License - see [LICENSE](https://github.com/jdtoon/CloudflareD1.NET/blob/master/LICENSE) for details
