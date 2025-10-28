# Migration Generation

One of the most powerful features of CloudflareD1.NET's Code-First approach is **automatic migration generation**. Instead of manually writing SQL migration scripts, you can define your models with attributes and let the framework generate the migrations for you.

## Overview

The migration generation feature:
- **Compares** your Code-First models with the current database schema
- **Detects** changes automatically (new tables, columns, indexes, foreign keys)
- **Generates** timestamped migration files with Up/Down methods
- **Maintains** migration history for version control

## Quick Start

### 1. Define Your Models

```csharp
using CloudflareD1.NET.CodeFirst.Attributes;

[Table("users")]
public class User
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Email { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

[Table("blog_posts")]
public class BlogPost
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    public string Content { get; set; }
    
    [ForeignKey(typeof(User), nameof(User.Id))]
    public int AuthorId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

### 2. Create Your DbContext

```csharp
public class BlogDbContext : D1Context
{
    public BlogDbContext(D1Client client) : base(client) { }
    
    public D1Set<User> Users { get; set; }
    public D1Set<BlogPost> BlogPosts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>();
        modelBuilder.Entity<BlogPost>();
    }
}
```

### 3. Generate Migration

Build your project first:
```bash
dotnet build --configuration Release
```

Then generate the migration:
```bash
dotnet d1 migrations add CreateBlogTables --code-first \
    --context YourNamespace.BlogDbContext \
    --assembly bin/Release/net8.0/YourApp.dll \
    --connection blog.db
```

### 4. Review Generated Migration

The CLI will create a migration file like `Migrations/20251028120000_CreateBlogTables.cs`:

```csharp
using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

public class Migration20251028120000_CreateBlogTables : Migration
{
    public override string Id => "20251028120000";
    public override string Name => "CreateBlogTables";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("username").NotNull();
            t.Text("email").NotNull();
            t.Text("created_at").NotNull();
        });

        builder.CreateTable("blog_posts", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("title").NotNull();
            t.Text("content");
            t.Integer("author_id").NotNull();
            t.Text("created_at").NotNull();
            t.ForeignKey("author_id", "users", "id");
        });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("users");
        builder.DropTable("blog_posts");
    }
}
```

### 5. Apply Migration

```bash
dotnet d1 migrations apply
```

## CLI Command Reference

### `migrations add` with `--code-first`

Generates a migration from Code-First models.

**Syntax:**
```bash
dotnet d1 migrations add <MigrationName> --code-first \
    --context <ContextTypeName> \
    --assembly <PathToAssembly> \
    --connection <DatabasePath>
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `MigrationName` | Yes | Name for the migration (e.g., `CreateUsersTable`) |
| `--code-first` | Yes | Enable Code-First migration generation |
| `--context` | Yes | Full type name of your `D1Context` (e.g., `MyApp.Data.BlogDbContext`) |
| `--assembly` | Yes | Path to the compiled assembly containing your models and context |
| `--connection` | Yes | Path to the SQLite database file |

**Example:**
```bash
dotnet d1 migrations add InitialCreate --code-first \
    --context BlogApp.Data.BlogDbContext \
    --assembly bin/Release/net8.0/BlogApp.dll \
    --connection blog.db
```

## Workflow

### Initial Migration

1. **Define models** with Code-First attributes
2. **Build** your project
3. **Generate migration**: `dotnet d1 migrations add InitialCreate --code-first ...`
4. **Review** the generated migration file
5. **Apply** migration: `dotnet d1 migrations apply`

### Subsequent Changes

After modifying your models:

1. **Update models** (add/remove properties, change attributes)
2. **Rebuild** project
3. **Generate new migration**: `dotnet d1 migrations add UpdateUserTable --code-first ...`
4. **Review** changes
5. **Apply**: `dotnet d1 migrations apply`

## Change Detection

The migration generator automatically detects:

### Table Changes
- ✅ New tables
- ✅ Dropped tables
- ✅ Renamed tables (future)

### Column Changes
- ✅ New columns
- ✅ Dropped columns
- ✅ Type changes (future)
- ✅ Constraint changes (future)

### Index Changes
- ✅ New indexes
- ✅ Dropped indexes

### Foreign Key Changes
- ✅ New foreign keys
- ✅ Dropped foreign keys

## Checking for Pending Changes

You can programmatically check if there are pending model changes:

```csharp
using CloudflareD1.NET.CodeFirst.MigrationGeneration;

var generator = new CodeFirstMigrationGenerator(client);

// Get a summary of changes
var summary = await generator.GetChangesSummaryAsync(context);
if (!string.IsNullOrEmpty(summary))
{
    Console.WriteLine("Pending changes:");
    Console.WriteLine(summary);
}
```

Or use the `ModelDiffer` for more control:

```csharp
using CloudflareD1.NET.CodeFirst.MigrationGeneration;

var differ = new ModelDiffer(client);
var metadata = context.GetModelMetadata();

// Quick check
if (await differ.HasChangesAsync(metadata))
{
    Console.WriteLine("Your models differ from the database!");
    
    // Get detailed comparison
    var (currentSchema, modelSchema) = await differ.CompareAsync(metadata);
    // Analyze differences...
}
```

## Best Practices

### 1. Always Review Generated Migrations

Before applying, review the migration to ensure:
- SQL is correct
- No unintended changes
- Foreign key constraints are in order
- Indexes are appropriate

### 2. Use Descriptive Migration Names

```bash
# Good
dotnet d1 migrations add AddUserEmailIndex --code-first ...
dotnet d1 migrations add UpdateBlogPostsContentType --code-first ...

# Bad
dotnet d1 migrations add Update1 --code-first ...
dotnet d1 migrations add Fix --code-first ...
```

### 3. Keep Migrations Small

Generate migrations frequently with focused changes rather than large, complex migrations.

### 4. Test Migrations Locally

Always test migrations in a local database before applying to production:

```bash
# Test with local database
dotnet d1 migrations apply --connection local.db

# Rollback if needed
dotnet d1 migrations revert
```

### 5. Version Control

Commit migration files to version control alongside your model changes:

```bash
git add Migrations/20251028120000_CreateBlogTables.cs
git add Models/User.cs
git commit -m "Add User model and initial migration"
```

## Example: Full Workflow

See the complete example in the repository:
- [samples/CodeFirst.Sample/](https://github.com/yourusername/CloudflareD1.NET/tree/master/samples/CodeFirst.Sample)

The sample demonstrates:
- Defining models with attributes
- Creating a DbContext
- Detecting pending changes
- Generating migrations
- Applying migrations
- Inspecting model metadata

To run the sample:

```bash
cd samples/CodeFirst.Sample
dotnet run --configuration Release
```

## Troubleshooting

### "Could not load assembly"

Ensure you've built the project before generating migrations:
```bash
dotnet build --configuration Release
dotnet d1 migrations add ... --assembly bin/Release/net8.0/YourApp.dll
```

### "Context type not found"

Use the full namespace and type name:
```bash
# Wrong
--context BlogDbContext

# Correct
--context MyCompany.BlogApp.Data.BlogDbContext
```

### "Database file not found"

Ensure the database path exists or use `:memory:` for in-memory databases:
```bash
--connection :memory:
```

### Navigation Properties in Generated Code

Currently, navigation properties (like `List<BlogPost>` on `User`) appear in generated migrations as `TEXT` columns. This is expected - they're stored in metadata but not persisted as actual database columns. Future versions will filter these automatically.

## Limitations

Current limitations (to be addressed in future releases):

- ❌ Navigation properties appear in schema (will be filtered)
- ❌ Column type changes not detected (will require manual migration)
- ❌ Column renames not detected (appears as drop + add)
- ❌ Complex types (owned entities, value objects)

## Next Steps

- Learn about [Migration Management](../migrations/overview.md)
- Explore [Code-First Attributes](./attributes.md)
- See [DbContext Configuration](./dbcontext.md)
