using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.MigrationGeneration;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("  DROP COLUMN Migration - End-to-End Test");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();

var migrationsDir = "Migrations";
if (Directory.Exists(migrationsDir))
{
    Directory.Delete(migrationsDir, true);
}
Directory.CreateDirectory(migrationsDir);

// Create client
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "test.db"
});

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Warning);
});

var client = new D1Client(options, loggerFactory.CreateLogger<D1Client>());

// Step 1: Create initial schema with price column
Console.WriteLine("Step 1: Creating initial schema with 'price' column");
Console.WriteLine("────────────────────────────────────────────────────────────────");

var context1 = new InitialContext(client);
var generator = new CodeFirstMigrationGenerator(migrationsDir);
var migration1Path = await generator.GenerateMigrationAsync(context1, "AddProducts", migrationsDir);

Console.WriteLine($"✓ Generated migration: {Path.GetFileName(migration1Path)}");

Console.WriteLine();

// Step 2: Remove price column
Console.WriteLine("Step 2: Removing 'price' column");
Console.WriteLine("────────────────────────────────────────────────────────────────");

var context2 = new UpdatedContext(client);
var migration2Path = await generator.GenerateMigrationAsync(context2, "RemovePrice", migrationsDir);

Console.WriteLine($"✓ Generated migration: {Path.GetFileName(migration2Path)}");

// Read and display the migration
var migrationCode = await File.ReadAllTextAsync(migration2Path);
Console.WriteLine();
Console.WriteLine("Migration code preview:");
Console.WriteLine("```csharp");
var lines = migrationCode.Split('\n').Skip(10).Take(20);
foreach (var line in lines)
{
    Console.WriteLine(line);
}
Console.WriteLine("```");
Console.WriteLine();

// Verify the migration uses table recreation pattern
if (!migrationCode.Contains("RenameTable"))
{
    Console.WriteLine("❌ FAILED: Migration doesn't use RenameTable!");
    Environment.Exit(1);
}

if (!migrationCode.Contains("DropTable(\"products_old\")"))
{
    Console.WriteLine("❌ FAILED: Migration doesn't drop old table!");
    Environment.Exit(1);
}

if (!migrationCode.Contains("INSERT INTO products"))
{
    Console.WriteLine("❌ FAILED: Migration doesn't copy data!");
    Environment.Exit(1);
}

Console.WriteLine("✓ Migration uses table recreation pattern");
Console.WriteLine("✓ Migration includes data copy");
Console.WriteLine("✓ Migration cleans up old table");

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("✅ DROP COLUMN migration generation works correctly!");
Console.WriteLine("════════════════════════════════════════════════════════════════");

// Cleanup
if (File.Exists("test.db"))
{
    File.Delete("test.db");
}

// Models
[Table("products")]
class ProductWithPrice
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    [Required] public decimal Price { get; set; }
}

[Table("products")]
class ProductWithoutPrice
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

class InitialContext : D1Context
{
    public InitialContext(D1Client client) : base(client) { }
    public D1Set<ProductWithPrice> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductWithPrice>();
    }
}

class UpdatedContext : D1Context
{
    public UpdatedContext(D1Client client) : base(client) { }
    public D1Set<ProductWithoutPrice> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductWithoutPrice>();
    }
}
