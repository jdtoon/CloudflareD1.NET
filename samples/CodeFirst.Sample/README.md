# CodeFirst.Sample - Blog Application

This sample demonstrates the complete Code-First workflow with automatic migration generation.

## Overview

A simple blog application with:
- **Users** - Application users who can write blog posts
- **BlogPosts** - Articles written by users
- **Comments** - Comments on blog posts

## Features Demonstrated

✅ Entity definition with attributes  
✅ DbContext with OnModelCreating  
✅ Automatic migration generation  
✅ Schema introspection and change detection  
✅ CRUD operations  
✅ Foreign key relationships  

## Quick Start

### 1. Build the Project

```bash
dotnet build --configuration Release
```

### 2. Run the Sample

```bash
dotnet run --project samples/CodeFirst.Sample
```

**Expected Output:**
```
╔═══════════════════════════════════════════════════════════════╗
║       CloudflareD1.NET Code-First Sample Application          ║
╚═══════════════════════════════════════════════════════════════╝

📊 Blog Database Context Created
   Database: blog.db

═══════════════════════════════════════════════════════════════
Step 1: Check for Pending Model Changes
═══════════════════════════════════════════════════════════════

⚠️  Pending model changes detected!

Pending model changes:

  + Create table 'users' with 6 column(s)
  + Create table 'blog_posts' with 6 column(s)
  + Create table 'comments' with 5 column(s)

💡 To generate a migration, run:
   dotnet d1 migrations add InitialCreate \
       --code-first \
       --context CodeFirst.Sample.BlogDbContext \
       --assembly bin/Release/net8.0/CodeFirst.Sample.dll
```

### 3. Generate Initial Migration

From the **repository root**:

```bash
dotnet d1 migrations add InitialCreate \
    --code-first \
    --context CodeFirst.Sample.BlogDbContext \
    --assembly samples/CodeFirst.Sample/bin/Release/net8.0/CodeFirst.Sample.dll \
    --connection samples/CodeFirst.Sample/blog.db
```

**Expected Output:**
```
🧮 Generating Code-First migration...

Loading context: CodeFirst.Sample.BlogDbContext...
✓ Discovered 3 entity type(s)
Pending model changes:

  + Create table 'users' with 6 column(s)
  + Create table 'blog_posts' with 6 column(s)
  + Create table 'comments' with 5 column(s)

✓ Created migration: 20251028120000_InitialCreate.cs
  Location: Migrations/20251028120000_InitialCreate.cs

Next steps:
  1. Review the generated migration
  2. Run 'dotnet d1 database update' to apply it
```

### 4. Review Generated Migration

The tool creates `Migrations/20251028120000_InitialCreate.cs`:

```csharp
using CloudflareD1.NET.Migrations;

public class Migration_20251028120000_InitialCreate : Migration
{
    public override string Id => "20251028120000";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("username").NotNull();
            t.Text("email").NotNull();
            t.Text("full_name");
            t.Integer("is_active");
            t.Text("created_at");
        });

        builder.CreateTable("blog_posts", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("title").NotNull();
            t.Text("content");
            t.Integer("author_id");
            t.Text("published_at");
            t.Text("created_at");
            t.ForeignKey("author_id", "users", "id");
        });

        builder.CreateTable("comments", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("content").NotNull();
            t.Integer("post_id");
            t.Integer("author_id");
            t.Text("created_at");
            t.ForeignKey("post_id", "blog_posts", "id");
            t.ForeignKey("author_id", "users", "id");
        });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("comments");
        builder.DropTable("blog_posts");
        builder.DropTable("users");
    }
}
```

### 5. Apply Migration

```bash
# From the sample directory
cd samples/CodeFirst.Sample
dotnet d1 database update
```

Or use the context's MigrateAsync method (already in Program.cs):

```bash
dotnet run --project samples/CodeFirst.Sample
```

### 6. Run Sample Again

After migration is applied:

```bash
dotnet run --project samples/CodeFirst.Sample
```

**Expected Output:**
```
Step 2: Apply Migrations
✅ Database is up to date - no migrations to apply

Step 3: Test CRUD Operations
📝 Creating sample data...
   ✓ User created: johndoe
   ✓ Blog post created

📊 Found 1 user(s) in database
   - johndoe (john@example.com)

📝 Found 1 blog post(s)
   - Getting Started with CloudflareD1.NET

Step 4: Model Metadata
📋 Discovered 3 entity types:

   User → users
      Properties: 6
      Primary Key: Id

   BlogPost → blog_posts
      Properties: 6
      Primary Key: Id
      Foreign Keys: 1
         - AuthorId → User

   Comment → comments
      Properties: 5
      Primary Key: Id
      Foreign Keys: 2
         - PostId → BlogPost
         - AuthorId → User
```

## Making Changes

### Add a New Property

1. **Modify `Models.cs`** - Add a property to User:

```csharp
[Column("bio")]
public string? Bio { get; set; }
```

2. **Build the project**:

```bash
dotnet build --configuration Release
```

3. **Generate migration**:

```bash
dotnet d1 migrations add AddUserBio \
    --code-first \
    --context CodeFirst.Sample.BlogDbContext \
    --assembly samples/CodeFirst.Sample/bin/Release/net8.0/CodeFirst.Sample.dll \
    --connection samples/CodeFirst.Sample/blog.db
```

**Output:**
```
Pending model changes:

  + Add column 'users.bio' (TEXT)

✓ Created migration: 20251028130000_AddUserBio.cs
```

4. **Apply migration**:

```bash
dotnet d1 database update
```

## Project Structure

```
CodeFirst.Sample/
├── CodeFirst.Sample.csproj
├── Program.cs              # Main application entry point
├── BlogDbContext.cs        # DbContext with entity configuration
├── Models.cs               # Entity classes (User, BlogPost, Comment)
├── README.md              # This file
├── blog.db                # SQLite database (created at runtime)
└── Migrations/            # Generated migration files
    ├── 20251028120000_InitialCreate.cs
    └── 20251028130000_AddUserBio.cs
```

## Key Concepts

### Entity Attributes

- `[Table("name")]` - Specify table name
- `[Key]` - Mark primary key
- `[Column("name")]` - Specify column name
- `[Required]` - NOT NULL constraint
- `[ForeignKey("PropertyName")]` - Define foreign key
- `[StringLength(100)]` - Documentation only (SQLite doesn't enforce)

### DbContext Pattern

```csharp
public class BlogDbContext : D1Context
{
    public D1Set<User> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities here
    }
}
```

### Migration Generation

The CodeFirstMigrationGenerator:
1. Reads your model metadata from DbContext
2. Introspects current database schema
3. Compares them to find differences
4. Generates migration code with Up() and Down()

## Comparison with EF Core

| EF Core | CloudflareD1.NET CodeFirst |
|---------|---------------------------|
| `DbContext` | `D1Context` |
| `DbSet<T>` | `D1Set<T>` |
| `Add-Migration` | `dotnet d1 migrations add --code-first` |
| `Update-Database` | `dotnet d1 database update` |
| `OnModelCreating` | `OnModelCreating` (same) |

## Troubleshooting

### "Could not find context type"

Use the fully qualified name:
```bash
--context CodeFirst.Sample.BlogDbContext
```

### "Assembly not found"

Build first and use correct path:
```bash
dotnet build --configuration Release
--assembly samples/CodeFirst.Sample/bin/Release/net8.0/CodeFirst.Sample.dll
```

### "No changes detected"

Your model matches the database. Make changes to entity classes and rebuild.

## Next Steps

- Try adding new entities
- Add indexes using `[Index]` attributes
- Experiment with different data types
- Test with Cloudflare D1 in production mode
