# Model Diff Sample

This example demonstrates how to generate migrations from Code-First model classes using the `dotnet d1 migrations diff` command.

## Overview

The `migrations diff` command analyzes your `D1Context` subclass and entity classes, then generates migrations that reflect your model schema. This enables a true Code-First workflow where you define your models in C# and let the tooling create the database schema.

## Project Structure

```
ModelDiffSample/
├── Model.cs              # Entity classes and DbContext
├── ModelDiffSample.csproj
├── Migrations/           # Generated migration files
└── .migrations-snapshot.json  # Schema snapshot
```

## Model Definition

The sample includes a simple `User` entity:

```csharp
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class AppDbContext : D1Context
{
    public D1Set<User> Users { get; set; } = null!;
}
```

## Workflow

### 1. Initial Migration

Generate your first migration from the model:

```bash
# Build the project
dotnet build

# Generate migration
dotnet d1 migrations diff InitialCreate --context ModelDiffSample.AppDbContext --assembly bin/Debug/net8.0/ModelDiffSample.dll
```

This creates:
- `Migrations/20241027HHMMSS_InitialCreate.cs` - Migration file with `CreateTable("users")`
- `.migrations-snapshot.json` - Snapshot of current schema

### 2. Review Generated Migration

Open the generated migration file to see the SQL operations:

```csharp
public class InitialCreate : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", table =>
        {
            table.Integer("id").PrimaryKey();
            table.Text("name").NotNull();
        });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("users");
    }
}
```

### 3. Make Model Changes

Add a new property to your entity:

```csharp
[Column("email")]
public string? Email { get; set; }
```

### 4. Generate Incremental Migration

```bash
# Rebuild
dotnet build

# Generate migration for the change
dotnet d1 migrations diff AddEmailColumn --context ModelDiffSample.AppDbContext --assembly bin/Debug/net8.0/ModelDiffSample.dll
```

This generates a new migration with only the difference:

```csharp
public class AddEmailColumn : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        builder.AddColumn("users", "email", "TEXT");
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropColumn("users", "email");
    }
}
```

## How It Works

1. **Model Discovery**: The CLI loads your assembly and reflects on the `D1Context` type
2. **Schema Building**: Analyzes `D1Set<T>` properties and entity attributes to build in-memory schema
3. **Comparison**: Compares the model schema to the last snapshot (`.migrations-snapshot.json`)
4. **Migration Generation**: Creates migration code for any differences
5. **Snapshot Update**: Updates the snapshot to match your current model

## Supported Attributes

- `[Table("name")]` - Specify table name (defaults to class name)
- `[Column("name")]` - Specify column name (defaults to property name)
- `[Key]` - Mark as primary key
- `[Required]` - Mark column as NOT NULL
- `[NotMapped]` - Exclude property from database
- `[ForeignKey("PropertyName")]` - Define foreign key (not yet used for schema generation)

## Type Mappings

| C# Type | SQLite Type |
|---------|-------------|
| `int`, `long` | INTEGER |
| `string` | TEXT |
| `bool` | INTEGER |
| `DateTime` | TEXT |
| `byte[]` | BLOB |
| `float`, `double`, `decimal` | REAL |
| `Guid` | TEXT |

## Current Limitations

- **Relationships**: Foreign key relationships are not yet detected or generated
- **OnModelCreating**: Fluent configuration in `OnModelCreating` is not yet invoked (use attributes)
- **Indexes**: Index definitions are not yet supported (use manual migrations)
- **Complex Types**: Value objects and owned entities are not supported

## Next Steps

After generating migrations:

1. **Review**: Always review generated migrations before applying
2. **Apply**: Use `dotnet d1 database update` to apply migrations to your database
3. **Test**: Verify the schema matches your expectations
4. **Commit**: Check in both migration files and `.migrations-snapshot.json`

## Full Example

```bash
# Clone the repository
git clone https://github.com/cloudflare/CloudflareD1.NET.git
cd CloudflareD1.NET/examples/ModelDiffSample

# Build the project
dotnet build

# Generate initial migration
dotnet d1 migrations diff InitialCreate --context ModelDiffSample.AppDbContext --assembly bin/Debug/net8.0/ModelDiffSample.dll

# Apply the migration (requires a D1 database or local SQLite)
dotnet d1 database update --connection local.db

# Modify Model.cs (add a property)
# Rebuild and generate incremental migration
dotnet build
dotnet d1 migrations diff AddNewProperty --context ModelDiffSample.AppDbContext --assembly bin/Debug/net8.0/ModelDiffSample.dll

# Review and apply
dotnet d1 database update --connection local.db
```

## See Also

- [Migrations Overview](../../docs/docs/migrations/overview.md)
- [Code-First Getting Started](../../docs/docs/code-first/getting-started.md)
- [Migration Builder API](../../docs/docs/migrations/overview.md#migration-builder-api)
