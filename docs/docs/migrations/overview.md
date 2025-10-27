---
sidebar_position: 1
---

# Database Migrations

CloudflareD1.NET.Migrations provides a robust database migration system for managing schema changes over time. It includes a powerful fluent API and CLI tool for creating and managing migrations.

## Overview

Migrations allow you to:
- **Version control** your database schema
- **Track changes** over time with migration history
- **Deploy safely** with automatic rollback support
- **Collaborate** with your team using consistent schema definitions

## Installation

Install the migrations package:

```bash
dotnet add package CloudflareD1.NET.Migrations
```

Install the CLI tool globally:

```bash
dotnet tool install -g dotnet-d1
```

## Quick Start

### 1. Create Your First Migration

```bash
dotnet d1 migrations add CreateUsersTable
```

This creates a new migration file with a timestamp:

```csharp
using CloudflareD1.NET.Migrations;

public class CreateUsersTable_20241027120000 : Migration
{
    public override string Id => "20241027120000";
    public override string Name => "CreateUsersTable";

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

        builder.CreateIndex("idx_users_email", "users", new[] { "email" }, unique: true);
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropIndex("idx_users_email");
        builder.DropTable("users");
    }
}
```

### 2. Apply Migrations

```bash
dotnet d1 database update
```

### 3. List Applied Migrations

```bash
dotnet d1 migrations list
```

### 4. Rollback Migrations

```bash
# Rollback the last migration
dotnet d1 database rollback

# Rollback to a specific migration
dotnet d1 database rollback --to 20241027120000
```

## Migration Builder API

The `MigrationBuilder` provides a fluent API for defining schema changes:

### Creating Tables

```csharp
public override void Up(MigrationBuilder builder)
{
    builder.CreateTable("posts", t =>
    {
        // Primary key with auto-increment
        t.Integer("id").PrimaryKey().AutoIncrement();
        
        // Required text fields
        t.Text("title").NotNull();
        t.Text("content").NotNull();
        
        // Optional field
        t.Text("excerpt");
        
        // Field with default value
        t.Text("status").Default("draft");
        
        // Numeric fields
        t.Integer("view_count").Default(0);
        t.Real("rating");
        
        // Blob field
        t.Blob("thumbnail");
        
        // Foreign key
        t.Integer("author_id").NotNull();
        t.ForeignKey("author_id", "users", "id", onDelete: "CASCADE");
        
        // Timestamps
        t.Text("created_at").Default("CURRENT_TIMESTAMP");
        t.Text("updated_at");
    });
}
```

### Column Types

```csharp
// Integer (whole numbers)
t.Integer("count").NotNull();

// Text (strings)
t.Text("name").NotNull();

// Real (floating point)
t.Real("price").NotNull();

// Blob (binary data)
t.Blob("data");
```

### Column Modifiers

```csharp
// Primary key
t.Integer("id").PrimaryKey();

// Auto-increment (only for INTEGER PRIMARY KEY)
t.Integer("id").PrimaryKey().AutoIncrement();

// Not null
t.Text("email").NotNull();

// Unique
t.Text("username").Unique();

// Default value
t.Text("status").Default("active");
t.Integer("count").Default(0);
```

### Table Constraints

```csharp
builder.CreateTable("order_items", t =>
{
    t.Integer("order_id").NotNull();
    t.Integer("product_id").NotNull();
    t.Integer("quantity").NotNull();
    
    // Composite primary key
    t.PrimaryKey("order_id", "product_id");
    
    // Foreign keys
    t.ForeignKey("order_id", "orders", "id", onDelete: "CASCADE");
    t.ForeignKey("product_id", "products", "id");
    
    // Unique constraint
    t.Unique("order_id", "product_id");
    
    // Check constraint
    t.Check("quantity > 0");
});
```

### Altering Tables

```csharp
// Add a column
builder.AddColumn("users", "phone", "TEXT", nullable: true);

// Add a column with constraints
builder.AddColumn("users", "status", "TEXT", nullable: false, defaultValue: "'active'");

// Drop a column
builder.DropColumn("users", "phone");

// Rename a column
builder.RenameColumn("users", "fullname", "full_name");

// Rename a table
builder.RenameTable("user", "users");
```

### Managing Indexes

```csharp
// Create a simple index
builder.CreateIndex("idx_users_email", "users", new[] { "email" });

// Create a unique index
builder.CreateIndex("idx_users_username", "users", new[] { "username" }, unique: true);

// Create a composite index
builder.CreateIndex("idx_users_name", "users", new[] { "first_name", "last_name" });

// Drop an index
builder.DropIndex("idx_users_email");
```

### Dropping Tables

```csharp
// Drop a table (with IF EXISTS by default)
builder.DropTable("old_table");

// Drop a table without IF EXISTS
builder.DropTable("old_table", ifExists: false);
```

### Raw SQL

```csharp
// Execute custom SQL
builder.Sql("INSERT INTO users (name, email) VALUES ('Admin', 'admin@example.com')");
builder.Sql("CREATE TRIGGER update_timestamp AFTER UPDATE ON posts BEGIN ... END");
```

## CLI Tool

### Installation

```bash
dotnet tool install -g dotnet-d1
```

### Commands

#### Create a New Migration

```bash
dotnet d1 migrations add <MigrationName>
```

Example:
```bash
dotnet d1 migrations add CreateUsersTable
dotnet d1 migrations add AddEmailIndexToUsers
dotnet d1 migrations add AddPostsTable
```

#### Scaffold from Database

Generate migrations automatically by comparing your database schema:

```bash
# Scaffold migration from database changes
dotnet d1 migrations scaffold <MigrationName> --connection <database-path>
```

Example workflow:
```bash
# 1. Make changes to your SQLite database
sqlite3 local.db "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT)"

# 2. Scaffold a migration from those changes
dotnet d1 migrations scaffold AddProductsTable --connection local.db

# 3. Review the generated migration file
# The tool will create Migrations/20241027120000_AddProductsTable.cs

# 4. Make more changes
sqlite3 local.db "ALTER TABLE products ADD COLUMN price REAL"

# 5. Scaffold again - only detects NEW changes
dotnet d1 migrations scaffold AddProductPrice --connection local.db
```

**How it works:**
- First scaffold creates a snapshot of your database schema
- Subsequent scaffolds compare current schema to the snapshot
- Only generates migrations for the differences
- Automatically creates `.migrations-snapshot.json` to track state

**What it detects:**
- ✅ New tables
- ✅ New columns (ALTER TABLE ADD COLUMN)
- ✅ Dropped tables
- ✅ New/dropped indexes
- ✅ Column types, constraints (PRIMARY KEY, NOT NULL, UNIQUE, DEFAULT)

#### List Migrations

```bash
# List all migrations with their status
dotnet d1 migrations list
```

Output:
```
Migrations:
✓ 20241027120000_CreateUsersTable (applied)
✓ 20241027130000_AddPostsTable (applied)
  20241027140000_AddCommentsTable (pending)
```

#### Apply Migrations

```bash
# Apply all pending migrations
dotnet d1 database update

# Apply migrations up to a specific migration
dotnet d1 database update --to 20241027130000
```

#### Rollback Migrations

```bash
# Rollback the last migration
dotnet d1 database rollback

# Rollback to a specific migration
dotnet d1 database rollback --to 20241027120000
```

## Programmatic Usage

You can also use migrations programmatically in your application:

### Setting Up the Migration Runner

```csharp
using CloudflareD1.NET;
using CloudflareD1.NET.Migrations;

// Create D1 client
var client = new D1Client(options, logger);

// Get all migrations from your assembly
var migrations = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
    .Select(t => (Migration)Activator.CreateInstance(t)!)
    .ToList();

// Create migration runner
var runner = new MigrationRunner(client, migrations);
```

### Applying Migrations

```csharp
// Apply all pending migrations
var appliedMigrations = await runner.MigrateAsync();
Console.WriteLine($"Applied {appliedMigrations.Count} migrations");

// Apply migrations up to a specific migration
var appliedMigrations = await runner.MigrateToAsync("20241027130000");

// Get pending migrations
var pendingMigrations = await runner.GetPendingMigrationsAsync();
Console.WriteLine($"{pendingMigrations.Count} migrations pending");

// Get applied migrations
var appliedMigrations = await runner.GetAppliedMigrationsAsync();
foreach (var migrationId in appliedMigrations)
{
    Console.WriteLine($"✓ {migrationId}");
}
```

### Rolling Back Migrations

```csharp
// Rollback the last migration
var rolledBackId = await runner.RollbackAsync();
if (rolledBackId != null)
{
    Console.WriteLine($"Rolled back migration: {rolledBackId}");
}

// Rollback to a specific migration
var rolledBackIds = await runner.RollbackToAsync("20241027120000");
Console.WriteLine($"Rolled back {rolledBackIds.Count} migrations");
```

## Migration History

Migrations are tracked in a special `__migrations` table:

| Column | Type | Description |
|--------|------|-------------|
| migration_id | TEXT | Unique timestamp-based migration ID |
| migration_name | TEXT | Human-readable migration name |
| applied_at | TEXT | Timestamp when migration was applied |

You can query this table directly if needed:

```sql
SELECT * FROM __migrations ORDER BY applied_at DESC;
```

## Best Practices

### 1. Never Modify Applied Migrations

Once a migration has been applied to production, never modify it. Instead, create a new migration to make changes:

❌ **Don't do this:**
```csharp
// Modifying an already applied migration
public override void Up(MigrationBuilder builder)
{
    builder.CreateTable("users", t => {
        t.Integer("id").PrimaryKey();
        t.Text("name").NotNull();
        // Adding email column later - DON'T DO THIS!
        t.Text("email").NotNull();
    });
}
```

✅ **Do this instead:**
```csharp
// Create a new migration
public override void Up(MigrationBuilder builder)
{
    builder.AddColumn("users", "email", "TEXT", nullable: false);
}
```

### 2. Always Implement Down()

Always implement the `Down()` method to allow rollbacks:

```csharp
public override void Up(MigrationBuilder builder)
{
    builder.CreateTable("users", t => { /* ... */ });
}

public override void Down(MigrationBuilder builder)
{
    builder.DropTable("users");
}
```

### 3. Use Descriptive Names

Use clear, descriptive names for your migrations:

✅ Good:
- `CreateUsersTable`
- `AddEmailIndexToUsers`
- `AddPostStatusColumn`

❌ Bad:
- `Migration1`
- `UpdateDatabase`
- `Fix`

### 4. Keep Migrations Small

Break large schema changes into smaller, focused migrations:

✅ Good:
```bash
dotnet d1 migrations add CreateUsersTable
dotnet d1 migrations add CreatePostsTable
dotnet d1 migrations add AddUserPostsRelationship
```

❌ Bad:
```bash
dotnet d1 migrations add CreateAllTables
```

### 5. Test Rollbacks

Always test that your migrations can be rolled back:

```bash
# Apply migration
dotnet d1 database update

# Test rollback
dotnet d1 database rollback

# Reapply
dotnet d1 database update
```

### 6. Use Transactions Wisely

Each migration runs in its own context. For complex migrations, ensure operations are idempotent:

```csharp
public override void Up(MigrationBuilder builder)
{
    // Use IF EXISTS for safety
    builder.DropTable("temp_table", ifExists: true);
    
    // Create table
    builder.CreateTable("new_table", t => { /* ... */ });
}
```

### 7. Handle Data Migrations Carefully

When renaming or restructuring columns, preserve data:

```csharp
public override void Up(MigrationBuilder builder)
{
    // 1. Add new column
    builder.AddColumn("users", "full_name", "TEXT");
    
    // 2. Copy data
    builder.Sql("UPDATE users SET full_name = first_name || ' ' || last_name");
    
    // 3. Drop old columns
    builder.DropColumn("users", "first_name");
    builder.DropColumn("users", "last_name");
}
```

## Common Patterns

### Adding a Timestamp Column to Existing Table

```csharp
public override void Up(MigrationBuilder builder)
{
    builder.AddColumn("users", "created_at", "TEXT", 
        nullable: false, 
        defaultValue: "CURRENT_TIMESTAMP");
}

public override void Down(MigrationBuilder builder)
{
    builder.DropColumn("users", "created_at");
}
```

### Creating a Junction Table

```csharp
public override void Up(MigrationBuilder builder)
{
    builder.CreateTable("user_roles", t =>
    {
        t.Integer("user_id").NotNull();
        t.Integer("role_id").NotNull();
        t.PrimaryKey("user_id", "role_id");
        t.ForeignKey("user_id", "users", "id", onDelete: "CASCADE");
        t.ForeignKey("role_id", "roles", "id", onDelete: "CASCADE");
    });
    
    builder.CreateIndex("idx_user_roles_user", "user_roles", new[] { "user_id" });
    builder.CreateIndex("idx_user_roles_role", "user_roles", new[] { "role_id" });
}
```

### Renaming a Column with Data Preservation

```csharp
public override void Up(MigrationBuilder builder)
{
    // SQLite requires a multi-step process
    builder.AddColumn("users", "email_address", "TEXT");
    builder.Sql("UPDATE users SET email_address = email");
    builder.DropColumn("users", "email");
    builder.RenameColumn("users", "email_address", "email");
}
```

## Troubleshooting

### Migration History Table

If you need to manually inspect or fix migration history:

```sql
-- View all applied migrations
SELECT * FROM __migrations ORDER BY applied_at;

-- Manually mark a migration as applied (use with caution!)
INSERT INTO __migrations (migration_id, migration_name, applied_at)
VALUES ('20241027120000', 'CreateUsersTable', datetime('now'));

-- Remove a migration from history (use with caution!)
DELETE FROM __migrations WHERE migration_id = '20241027120000';
```

### Reset All Migrations

To completely reset your database (⚠️ **this will delete all data**):

```sql
-- Drop all tables
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS posts;
-- ... drop all your tables

-- Drop migration history
DROP TABLE IF EXISTS __migrations;
```

Then reapply all migrations:

```bash
dotnet d1 database update
```

## See Also

- [Getting Started](../getting-started/installation.md)
- [Quick Start Guide](../getting-started/quick-start.md)
- [LINQ Package](../linq/intro.md)
