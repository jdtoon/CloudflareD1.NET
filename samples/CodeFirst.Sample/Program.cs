using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst.MigrationGeneration;
using CloudflareD1.NET.Configuration;
using CodeFirst.Sample;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        CloudflareD1.NET Code-First Sample Application         ║");
Console.WriteLine("║            Demonstrating Migration Generation                  ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Setup
const string dbPath = "blog.db";
var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = dbPath
});

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<D1Client>();
using var client = new D1Client(options, logger);

// Create context
var context = new BlogDbContext(client);

Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("Step 1: Check for Pending Model Changes");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();

var generator = new CodeFirstMigrationGenerator(client);
var summary = await generator.GetChangesSummaryAsync(context);

if (!string.IsNullOrEmpty(summary))
{
    Console.WriteLine("⚠️  Model changes detected!");
    Console.WriteLine("   Your entity models differ from the current database schema.");
    Console.WriteLine();
    Console.WriteLine("📋 Changes Summary:");
    Console.WriteLine(summary);
    Console.WriteLine();
    Console.WriteLine("💡 To generate a migration, run:");
    Console.WriteLine($"   dotnet d1 migrations add InitialCreate --code-first \\");
    Console.WriteLine($"      --context CodeFirst.Sample.BlogDbContext \\");
    Console.WriteLine($"      --assembly bin/Release/net8.0/CodeFirst.Sample.dll \\");
    Console.WriteLine($"      --connection {dbPath}");
    Console.WriteLine();
}
else
{
    Console.WriteLine("✅ No pending model changes");
    Console.WriteLine("   Database schema matches your entity models perfectly.");
    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("Step 2: Apply Migrations");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();

var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");
if (Directory.Exists(migrationsPath) && Directory.GetFiles(migrationsPath, "*.cs").Length > 0)
{
    Console.WriteLine("📁 Found migration files:");
    foreach (var file in Directory.GetFiles(migrationsPath, "*.cs"))
    {
        Console.WriteLine($"   - {Path.GetFileName(file)}");
    }
    Console.WriteLine();
    Console.WriteLine("💡 To apply migrations, run:");
    Console.WriteLine("   dotnet d1 migrations apply");
    Console.WriteLine();
}
else
{
    Console.WriteLine("ℹ️  No migrations found yet.");
    Console.WriteLine("   Generate your first migration using the command shown above.");
    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("Step 3: Inspect Model Metadata");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();

var metadata = context.GetModelMetadata();
Console.WriteLine($"📊 Your BlogDbContext contains {metadata.Entities.Count} entity types:");
Console.WriteLine();

foreach (var (entityType, entityMetadata) in metadata.Entities)
{
    Console.WriteLine($"🔹 {entityMetadata.TableName} ({entityType.Name})");
    Console.WriteLine($"   Properties: {entityMetadata.Properties.Count}");

    foreach (var prop in entityMetadata.Properties)
    {
        var flags = new List<string>();
        if (prop.IsPrimaryKey) flags.Add("PK");
        if (prop.IsRequired) flags.Add("Required");
        if (prop.IsAutoIncrement) flags.Add("AutoIncrement");

        var flagStr = flags.Any() ? $" [{string.Join(", ", flags)}]" : "";
        var colType = prop.ColumnType ?? prop.PropertyInfo.PropertyType.Name;
        Console.WriteLine($"      • {prop.ColumnName}: {colType}{flagStr}");
    }

    if (entityMetadata.ForeignKeys.Any())
    {
        Console.WriteLine($"   Foreign Keys: {entityMetadata.ForeignKeys.Count}");
    }

    if (entityMetadata.Indexes.Any())
    {
        Console.WriteLine($"   Indexes: {entityMetadata.Indexes.Count}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("Next Steps");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("1. Generate a migration if you saw pending changes:");
Console.WriteLine("   dotnet d1 migrations add InitialCreate --code-first \\");
Console.WriteLine("      --context CodeFirst.Sample.BlogDbContext \\");
Console.WriteLine("      --assembly bin/Release/net8.0/CodeFirst.Sample.dll \\");
Console.WriteLine($"      --connection {dbPath}");
Console.WriteLine();
Console.WriteLine("2. Apply the migration:");
Console.WriteLine("   dotnet d1 migrations apply");
Console.WriteLine();
Console.WriteLine("3. Run this sample again to verify the changes!");
Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine();
