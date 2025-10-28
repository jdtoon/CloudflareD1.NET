using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.MigrationGeneration;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     Migration Scenarios - Comprehensive Testing Suite         ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var testResults = new List<(string Scenario, bool Passed, string? Error)>();

// Test Scenario 1: Create Initial Schema
await RunScenario("Scenario 1: Initial Schema Creation", async () =>
{
    var context = new Scenario1Context(CreateClient("scenario1.db"));
    var migrationsDir = "Migrations/Scenario1";
    CleanMigrations(migrationsDir);

    var generator = new CodeFirstMigrationGenerator(migrationsDir);

    // Generate migration
    var path = await generator.GenerateMigrationAsync(context, "InitialCreate", migrationsDir);

    // Verify snapshot exists
    if (!File.Exists(Path.Combine(migrationsDir, ".migrations-snapshot.json")))
        throw new Exception("Snapshot not created");

    // Verify migration file
    var migrationCode = await File.ReadAllTextAsync(path);
    if (!migrationCode.Contains("CreateTable(\"products\""))
        throw new Exception("Migration doesn't contain CreateTable for products");

    // Verify Up contains CreateTable
    if (!migrationCode.Contains("public override void Up(MigrationBuilder builder)"))
        throw new Exception("Migration doesn't have Up method");

    // Verify Down contains DropTable
    if (!migrationCode.Contains("public override void Down(MigrationBuilder builder)"))
        throw new Exception("Migration doesn't have Down method");

    Console.WriteLine($"  ✓ Migration file created: {Path.GetFileName(path)}");
    Console.WriteLine($"  ✓ Snapshot created");
});

// Test Scenario 2: Add New Table
await RunScenario("Scenario 2: Add New Table", async () =>
{
    var context = new Scenario2Context(CreateClient("scenario2.db"));
    var migrationsDir = "Migrations/Scenario2";
    CleanMigrations(migrationsDir);

    var generator = new CodeFirstMigrationGenerator(migrationsDir);

    // First migration - create products
    await generator.GenerateMigrationAsync(context, "AddProducts", migrationsDir);

    // Second migration - add categories (modify context to include it)
    var context2 = new Scenario2WithCategoriesContext(CreateClient("scenario2.db"));
    var path2 = await generator.GenerateMigrationAsync(context2, "AddCategories", migrationsDir);

    var migrationCode = await File.ReadAllTextAsync(path2);

    // Should only create categories table, not recreate products
    if (!migrationCode.Contains("CreateTable(\"categories\""))
        throw new Exception("Second migration doesn't create categories table");

    if (migrationCode.Contains("CreateTable(\"products\""))
        throw new Exception("Second migration recreates products table!");

    Console.WriteLine($"  ✓ Second migration only adds new table");
    Console.WriteLine($"  ✓ Existing tables not recreated");
});

// Test Scenario 3: Add Column
await RunScenario("Scenario 3: Add Column to Existing Table", async () =>
{
    var migrationsDir = "Migrations/Scenario3";
    CleanMigrations(migrationsDir);

    // First migration
    var context1 = new Scenario3InitialContext(CreateClient("scenario3.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);
    await generator.GenerateMigrationAsync(context1, "InitialCreate", migrationsDir);

    // Second migration - add description column
    var context2 = new Scenario3WithDescriptionContext(CreateClient("scenario3.db"));
    var summary = await generator.GetChangesSummaryAsync(context2);

    if (!summary.Contains("+ Add column 'products.description'"))
        throw new Exception($"Changes summary doesn't show column addition. Got: {summary}");

    var path2 = await generator.GenerateMigrationAsync(context2, "AddDescription", migrationsDir);
    var migrationCode = await File.ReadAllTextAsync(path2);

    Console.WriteLine($"  ✓ Change detection works for new columns");
    Console.WriteLine($"  ✓ Migration generated for column addition");
});

// Test Scenario 4: Remove Column
await RunScenario("Scenario 4: Remove Column from Existing Table", async () =>
{
    var migrationsDir = "Migrations/Scenario4";
    CleanMigrations(migrationsDir);

    // First migration with price column
    var context1 = new Scenario4WithPriceContext(CreateClient("scenario4.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);
    await generator.GenerateMigrationAsync(context1, "InitialCreate", migrationsDir);

    // Second migration - remove price column
    var context2 = new Scenario4WithoutPriceContext(CreateClient("scenario4.db"));
    var summary = await generator.GetChangesSummaryAsync(context2);

    if (!summary.Contains("- Drop column 'products.price'"))
        throw new Exception($"Changes summary doesn't show column removal. Got: {summary}");

    await generator.GenerateMigrationAsync(context2, "RemovePrice", migrationsDir);

    Console.WriteLine($"  ✓ Change detection works for removed columns");
    Console.WriteLine($"  ✓ Migration generated for column removal");
});

// Test Scenario 5: Add Foreign Key
await RunScenario("Scenario 5: Add Foreign Key Relationship", async () =>
{
    var migrationsDir = "Migrations/Scenario5";
    CleanMigrations(migrationsDir);

    // First migration - tables with all columns including FK column
    var context1 = new Scenario5NoFKContext(CreateClient("scenario5.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);
    await generator.GenerateMigrationAsync(context1, "InitialCreate", migrationsDir);

    // Second migration - define FK relationship
    var context2 = new Scenario5WithFKContext(CreateClient("scenario5.db"));
    var hasChanges = await generator.HasPendingChangesAsync(context2);

    if (!hasChanges)
        throw new Exception("No changes detected when FK was added");

    var summary = await generator.GetChangesSummaryAsync(context2);

    // Should detect the foreign key addition
    if (!summary.Contains("+ Add foreign key"))
        throw new Exception($"Changes summary doesn't show FK addition. Got: {summary}");

    await generator.GenerateMigrationAsync(context2, "AddProductFK", migrationsDir);

    Console.WriteLine($"  ✓ Change detection works for foreign keys");
    Console.WriteLine($"  ✓ Migration generated for FK addition");
});

// Test Scenario 6: No Changes
await RunScenario("Scenario 6: No Changes (Idempotency)", async () =>
{
    var migrationsDir = "Migrations/Scenario6";
    CleanMigrations(migrationsDir);

    var context = new Scenario6Context(CreateClient("scenario6.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);

    // First migration
    await generator.GenerateMigrationAsync(context, "InitialCreate", migrationsDir);

    // Try to generate again with no model changes
    var hasChanges = await generator.HasPendingChangesAsync(context);

    if (hasChanges)
        throw new Exception("Generator reports changes when there are none!");

    Console.WriteLine($"  ✓ No false positives for unchanged models");
});

// Test Scenario 7: Multiple Changes at Once
await RunScenario("Scenario 7: Multiple Changes in One Migration", async () =>
{
    var migrationsDir = "Migrations/Scenario7";
    CleanMigrations(migrationsDir);

    // First migration
    var context1 = new Scenario7InitialContext(CreateClient("scenario7.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);
    await generator.GenerateMigrationAsync(context1, "InitialCreate", migrationsDir);

    // Second migration with multiple changes
    var context2 = new Scenario7MultipleChangesContext(CreateClient("scenario7.db"));
    var summary = await generator.GetChangesSummaryAsync(context2);

    // Should detect: new table, new column, dropped column
    if (!summary.Contains("+ Create table 'reviews'"))
        throw new Exception("Doesn't detect new table");

    if (!summary.Contains("+ Add column 'products.stock'"))
        throw new Exception("Doesn't detect new column");

    if (!summary.Contains("- Drop column 'products.name'"))
        throw new Exception("Doesn't detect dropped column");

    await generator.GenerateMigrationAsync(context2, "MultipleChanges", migrationsDir);

    Console.WriteLine($"  ✓ Multiple changes detected correctly");
});

// Test Scenario 8: Snapshot Consistency
await RunScenario("Scenario 8: Snapshot Remains Consistent", async () =>
{
    var migrationsDir = "Migrations/Scenario8";
    CleanMigrations(migrationsDir);

    var context = new Scenario8Context(CreateClient("scenario8.db"));
    var generator = new CodeFirstMigrationGenerator(migrationsDir);

    // Generate migration
    await generator.GenerateMigrationAsync(context, "InitialCreate", migrationsDir);

    // Read snapshot
    var snapshotPath = Path.Combine(migrationsDir, ".migrations-snapshot.json");
    var snapshot1 = await File.ReadAllTextAsync(snapshotPath);

    // Generate again (should have no changes)
    var hasChanges = await generator.HasPendingChangesAsync(context);
    var snapshot2 = await File.ReadAllTextAsync(snapshotPath);

    if (snapshot1 != snapshot2)
        throw new Exception("Snapshot changed when no model changes occurred!");

    if (hasChanges)
        throw new Exception("False positive change detection");

    Console.WriteLine($"  ✓ Snapshot remains stable");
    Console.WriteLine($"  ✓ No spurious changes detected");
});

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("Test Results Summary");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();

var passed = testResults.Count(r => r.Passed);
var failed = testResults.Count(r => !r.Passed);

foreach (var result in testResults)
{
    var status = result.Passed ? "✓ PASS" : "✗ FAIL";
    var color = result.Passed ? "" : " ⚠️";
    Console.WriteLine($"{status}{color} - {result.Scenario}");
    if (!result.Passed && result.Error != null)
    {
        Console.WriteLine($"        Error: {result.Error}");
    }
}

Console.WriteLine();
Console.WriteLine($"Total: {testResults.Count} | Passed: {passed} | Failed: {failed}");
Console.WriteLine();

if (failed > 0)
{
    Console.WriteLine("❌ Some tests failed!");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("✅ All tests passed!");
}

// Helper functions
async Task RunScenario(string name, Func<Task> test)
{
    Console.WriteLine();
    Console.WriteLine($"▶ {name}");
    Console.WriteLine(new string('─', 64));

    try
    {
        await test();
        testResults.Add((name, true, null));
        Console.WriteLine($"  ✅ PASSED");
    }
    catch (Exception ex)
    {
        testResults.Add((name, false, ex.Message));
        Console.WriteLine($"  ❌ FAILED: {ex.Message}");
    }
}

void CleanMigrations(string dir)
{
    if (Directory.Exists(dir))
    {
        foreach (var file in Directory.GetFiles(dir))
        {
            File.Delete(file);
        }
    }
    else
    {
        Directory.CreateDirectory(dir);
    }
}

D1Client CreateClient(string dbPath)
{
    var options = Options.Create(new D1Options
    {
        UseLocalMode = true,
        LocalDatabasePath = $":memory:" // Use in-memory for testing
    });

    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Warning);
    });

    return new D1Client(options, loggerFactory.CreateLogger<D1Client>());
}

// Scenario 1: Initial schema
[Table("products")]
class Product1
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

class Scenario1Context : D1Context
{
    public Scenario1Context(D1Client client) : base(client) { }
    public D1Set<Product1> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product1>();
    }
}

// Scenario 2: Add new table
[Table("products")]
class Product2
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

[Table("categories")]
class Category2
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

class Scenario2Context : D1Context
{
    public Scenario2Context(D1Client client) : base(client) { }
    public D1Set<Product2> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product2>();
    }
}

class Scenario2WithCategoriesContext : D1Context
{
    public Scenario2WithCategoriesContext(D1Client client) : base(client) { }
    public D1Set<Product2> Products { get; set; } = null!;
    public D1Set<Category2> Categories { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product2>();
        modelBuilder.Entity<Category2>();
    }
}

// Scenario 3: Add column
[Table("products")]
class Product3Initial
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

[Table("products")]
class Product3WithDescription
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public string? Description { get; set; }
}

class Scenario3InitialContext : D1Context
{
    public Scenario3InitialContext(D1Client client) : base(client) { }
    public D1Set<Product3Initial> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product3Initial>();
    }
}

class Scenario3WithDescriptionContext : D1Context
{
    public Scenario3WithDescriptionContext(D1Client client) : base(client) { }
    public D1Set<Product3WithDescription> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product3WithDescription>();
    }
}

// Scenario 4: Remove column
[Table("products")]
class Product4WithPrice
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

[Table("products")]
class Product4WithoutPrice
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

class Scenario4WithPriceContext : D1Context
{
    public Scenario4WithPriceContext(D1Client client) : base(client) { }
    public D1Set<Product4WithPrice> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product4WithPrice>();
    }
}

class Scenario4WithoutPriceContext : D1Context
{
    public Scenario4WithoutPriceContext(D1Client client) : base(client) { }
    public D1Set<Product4WithoutPrice> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product4WithoutPrice>();
    }
}

// Scenario 5: Add foreign key
[Table("products")]
class Product5
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

[Table("order_items")]
class OrderItem5NoFK
{
    [Key] public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

[Table("order_items")]
class OrderItem5WithFK
{
    [Key] public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Product5? Product { get; set; }
}

class Scenario5NoFKContext : D1Context
{
    public Scenario5NoFKContext(D1Client client) : base(client) { }
    public D1Set<Product5> Products { get; set; } = null!;
    public D1Set<OrderItem5NoFK> OrderItems { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product5>();
        modelBuilder.Entity<OrderItem5NoFK>();
    }
}

class Scenario5WithFKContext : D1Context
{
    public Scenario5WithFKContext(D1Client client) : base(client) { }
    public D1Set<Product5> Products { get; set; } = null!;
    public D1Set<OrderItem5WithFK> OrderItems { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product5>();
        modelBuilder.Entity<OrderItem5WithFK>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId);
    }
}

// Scenario 6: No changes
[Table("products")]
class Product6
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

class Scenario6Context : D1Context
{
    public Scenario6Context(D1Client client) : base(client) { }
    public D1Set<Product6> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product6>();
    }
}

// Scenario 7: Multiple changes
[Table("products")]
class Product7Initial
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

[Table("products")]
class Product7Changed
{
    [Key] public int Id { get; set; }
    // Name removed
    public int Stock { get; set; } // Added
}

[Table("reviews")]
class Review7
{
    [Key] public int Id { get; set; }
    [Required] public string Text { get; set; } = "";
}

class Scenario7InitialContext : D1Context
{
    public Scenario7InitialContext(D1Client client) : base(client) { }
    public D1Set<Product7Initial> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product7Initial>();
    }
}

class Scenario7MultipleChangesContext : D1Context
{
    public Scenario7MultipleChangesContext(D1Client client) : base(client) { }
    public D1Set<Product7Changed> Products { get; set; } = null!;
    public D1Set<Review7> Reviews { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product7Changed>();
        modelBuilder.Entity<Review7>();
    }
}

// Scenario 8: Snapshot consistency
[Table("products")]
class Product8
{
    [Key] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

class Scenario8Context : D1Context
{
    public Scenario8Context(D1Client client) : base(client) { }
    public D1Set<Product8> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product8>();
    }
}
