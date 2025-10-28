# CloudflareD1.NET.Migrations

[![NuGet](https://img.shields.io/nuget/v/CloudflareD1.NET.Migrations.svg)](https://www.nuget.org/packages/CloudflareD1.NET.Migrations/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Database migration system for CloudflareD1.NET with fluent API and version tracking.

## Features

- ✅ **Fluent Migration API**: Chain methods to build complex migrations
- ✅ **Version Tracking**: Automatic history in `__migrations` table
- ✅ **Up/Down Migrations**: Full rollback support
- ✅ **Type-Safe Schema Building**: Strongly typed column definitions
- ✅ **Full DDL Support**: CREATE/ALTER/DROP tables, indexes, constraints
- ✅ **Programmatic & CLI**: Use in code or via CLI tool
- ✅ **Well Tested**: 29 unit tests + integration tests

## Installation

```bash
dotnet add package CloudflareD1.NET.Migrations
```

For CLI tool:
```bash
dotnet tool install -g dotnet-d1
```

## Quick Start

### Create a Migration

```csharp
using CloudflareD1.NET.Migrations;

public class CreateUsersTable : Migration
{
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

        builder.CreateIndex("users", "idx_users_email", "email");
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("users");
    }
}
```

### Run Migrations Programmatically

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.Migrations;
using System.Reflection;

var client = new D1Client(config);
var runner = new MigrationRunner(client, Assembly.GetExecutingAssembly());

// Apply all pending migrations
await runner.MigrateAsync();

// Rollback last migration
await runner.RollbackAsync();

// Get migration history
var history = await runner.GetAppliedMigrationsAsync();
```

### CLI Usage

```bash
# Add a new migration
dotnet d1 migrations add CreateUsersTable

# Apply pending migrations
dotnet d1 migrations update

# List migrations
dotnet d1 migrations list

# Rollback last migration
dotnet d1 migrations rollback
```

## Migration API

### Table Operations
- `CreateTable(name, columns)` - Create new table
- `DropTable(name)` - Drop existing table
- `AlterTable(name, columns)` - Modify table structure

### Column Types
- `Integer(name)` - Integer column
- `Real(name)` - Floating point column
- `Text(name)` - Text/string column
- `Blob(name)` - Binary data column

### Column Modifiers
- `.PrimaryKey()` - Mark as primary key
- `.AutoIncrement()` - Auto-incrementing values
- `.NotNull()` - NOT NULL constraint
- `.Unique()` - UNIQUE constraint
- `.Default(value)` - Default value

### Index Operations
- `CreateIndex(table, name, column)` - Create index
- `CreateUniqueIndex(table, name, column)` - Create unique index
- `DropIndex(table, name)` - Drop index

### Raw SQL
- `ExecuteRaw(sql)` - Execute custom SQL

## Related Packages

- **[CloudflareD1.NET](https://www.nuget.org/packages/CloudflareD1.NET/)** - Core database client
- **[CloudflareD1.NET.Linq](https://www.nuget.org/packages/CloudflareD1.NET.Linq/)** - LINQ query builder
- **[CloudflareD1.NET.CodeFirst](https://www.nuget.org/packages/CloudflareD1.NET.CodeFirst/)** - Code-First ORM with DbContext pattern and entity relationships
- **[dotnet-d1](https://www.nuget.org/packages/dotnet-d1/)** - CLI tool for migrations

## Documentation

Full documentation is available at [https://jdtoon.github.io/CloudflareD1.NET/docs/migrations/overview](https://jdtoon.github.io/CloudflareD1.NET/docs/migrations/overview)

## License

MIT License - see [LICENSE](https://github.com/jdtoon/CloudflareD1.NET/blob/master/LICENSE) for details
