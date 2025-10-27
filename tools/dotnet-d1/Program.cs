using System.CommandLine;
using System.Text;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Migrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetD1;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Cloudflare D1 Migrations Tool");

        // migrations add command
        var addCommand = new Command("add", "Create a new migration file");
        var nameArgument = new Argument<string>("name", "Name of the migration (e.g., CreateUsersTable)");
        addCommand.AddArgument(nameArgument);
        addCommand.SetHandler(async (string name) => await AddMigration(name), nameArgument);

        // migrations list command
        var listCommand = new Command("list", "List all migrations and their status");
        listCommand.SetHandler(async () => await ListMigrations());

        // migrations scaffold command
        var scaffoldCommand = new Command("scaffold", "Generate a migration from database schema changes");
        var scaffoldNameArg = new Argument<string>("name", "Name of the migration (e.g., AddUserColumns)");
        var connectionOption = new Option<string>("--connection", "SQLite database connection string or file path") { IsRequired = true };
        scaffoldCommand.AddArgument(scaffoldNameArg);
        scaffoldCommand.AddOption(connectionOption);
        scaffoldCommand.SetHandler(async (string name, string connection) => await ScaffoldMigration(name, connection), scaffoldNameArg, connectionOption);

        // database update command
        var updateCommand = new Command("update", "Apply all pending migrations");
        var targetOption = new Option<string?>("--target", "Target migration to update to");
        updateCommand.AddOption(targetOption);
        updateCommand.SetHandler(async (string? target) => await UpdateDatabase(target), targetOption);

        // database rollback command
        var rollbackCommand = new Command("rollback", "Rollback the last migration or to a specific migration");
        var rollbackTargetOption = new Option<string?>("--target", "Target migration to rollback to");
        rollbackCommand.AddOption(rollbackTargetOption);
        rollbackCommand.SetHandler(async (string? target) => await RollbackDatabase(target), rollbackTargetOption);

        // migrations command group
        var migrationsCommand = new Command("migrations", "Manage database migrations");
        migrationsCommand.AddCommand(addCommand);
        migrationsCommand.AddCommand(listCommand);
        migrationsCommand.AddCommand(scaffoldCommand);

        // database command group
        var databaseCommand = new Command("database", "Manage database state");
        databaseCommand.AddCommand(updateCommand);
        databaseCommand.AddCommand(rollbackCommand);

        rootCommand.AddCommand(migrationsCommand);
        rootCommand.AddCommand(databaseCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task AddMigration(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Error: Migration name cannot be empty.");
            return;
        }

        // Generate migration ID (timestamp: YYYYMMDDHHMMSS)
        var migrationId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var className = ToPascalCase(name);
        var fileName = $"{migrationId}_{className}.cs";

        // Find Migrations directory or create it
        var migrationsDir = FindOrCreateMigrationsDirectory();
        if (migrationsDir == null)
        {
            Console.WriteLine("Error: Could not find or create Migrations directory.");
            return;
        }

        var filePath = Path.Combine(migrationsDir, fileName);

        // Generate migration file content
        var content = GenerateMigrationFile(migrationId, className);

        await File.WriteAllTextAsync(filePath, content);

        Console.WriteLine($"✓ Created migration: {fileName}");
        Console.WriteLine($"  Location: {filePath}");
    }

    static async Task ListMigrations()
    {
        Console.WriteLine("📋 Migrations:");
        Console.WriteLine();

        var migrationsDir = FindMigrationsDirectory();
        if (migrationsDir == null)
        {
            Console.WriteLine("No Migrations directory found.");
            return;
        }

        var files = Directory.GetFiles(migrationsDir, "*_*.cs")
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No migrations found.");
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('_', 2);
            if (parts.Length == 2)
            {
                Console.WriteLine($"  [{parts[0]}] {parts[1]}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {files.Count} migration(s)");
    }

    static async Task UpdateDatabase(string? target)
    {
        Console.WriteLine("🚀 Applying migrations...");
        Console.WriteLine();

        if (target != null)
        {
            Console.WriteLine($"Target: {target}");
        }
        else
        {
            Console.WriteLine("Target: Latest");
        }

        Console.WriteLine();
        Console.WriteLine("To run migrations programmatically, use:");
        Console.WriteLine("  var runner = new MigrationRunner(client, migrations);");
        Console.WriteLine("  await runner.MigrateAsync();");
        Console.WriteLine();
        Console.WriteLine("Note: This CLI tool generates migration files.");
        Console.WriteLine("      Execute migrations in your application code.");
    }

    static async Task RollbackDatabase(string? target)
    {
        Console.WriteLine("⏪ Rolling back migrations...");
        Console.WriteLine();

        if (target != null)
        {
            Console.WriteLine($"Target: {target}");
        }
        else
        {
            Console.WriteLine("Rolling back last migration");
        }

        Console.WriteLine();
        Console.WriteLine("To rollback migrations programmatically, use:");
        Console.WriteLine("  var runner = new MigrationRunner(client, migrations);");
        if (target != null)
        {
            Console.WriteLine($"  await runner.RollbackToAsync(\"{target}\");");
        }
        else
        {
            Console.WriteLine("  await runner.RollbackAsync();");
        }
        Console.WriteLine();
        Console.WriteLine("Note: This CLI tool generates migration files.");
        Console.WriteLine("      Execute rollbacks in your application code.");
    }

    static string? FindMigrationsDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Look for Migrations directory in current directory
        var migrationsDir = Path.Combine(currentDir, "Migrations");
        if (Directory.Exists(migrationsDir))
            return migrationsDir;

        // Look in parent directories (up to 3 levels)
        for (int i = 0; i < 3; i++)
        {
            currentDir = Path.GetDirectoryName(currentDir);
            if (currentDir == null) break;

            migrationsDir = Path.Combine(currentDir, "Migrations");
            if (Directory.Exists(migrationsDir))
                return migrationsDir;
        }

        return null;
    }

    static string? FindOrCreateMigrationsDirectory()
    {
        var existing = FindMigrationsDirectory();
        if (existing != null)
            return existing;

        // Create in current directory
        var migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "Migrations");
        Directory.CreateDirectory(migrationsDir);
        return migrationsDir;
    }

    static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpper(word[0]));
                if (word.Length > 1)
                {
                    result.Append(word.Substring(1).ToLower());
                }
            }
        }

        return result.ToString();
    }

    static string GenerateMigrationFile(string migrationId, string className)
    {
        return $@"using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: {className}
/// Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
/// </summary>
public class Migration{migrationId}_{className} : Migration
{{
    public override string Id => ""{migrationId}"";
    public override string Name => ""{className}"";

    public override void Up(MigrationBuilder builder)
    {{
        // TODO: Define your migration here
        // Example:
        // builder.CreateTable(""users"", t => t
        //     .Integer(""id"").PrimaryKey().AutoIncrement()
        //     .Text(""name"").NotNull()
        //     .Text(""email"").NotNull().Unique()
        // );
    }}

    public override void Down(MigrationBuilder builder)
    {{
        // TODO: Define rollback logic here
        // Example:
        // builder.DropTable(""users"");
    }}
}}
";
    }

    static async Task ScaffoldMigration(string name, string connection)
    {
        try
        {
            Console.WriteLine("🔍 Scaffolding migration from database schema...");
            Console.WriteLine();

            // Normalize connection string
            if (!connection.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                connection = $"Data Source={connection}";
            }

            // Create D1Client for local SQLite
            var options = Options.Create(new D1Options
            {
                UseLocalMode = true,
                LocalDatabasePath = connection.Replace("Data Source=", "").Trim()
            });

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var logger = loggerFactory.CreateLogger<D1Client>();

            var client = new D1Client(options, logger);
            var introspector = new SchemaIntrospector(client);

            // Get current database schema
            Console.WriteLine("Reading database schema...");
            var currentSchema = await introspector.GetSchemaAsync();
            Console.WriteLine($"✓ Found {currentSchema.Tables.Count} table(s)");
            Console.WriteLine();

            // Load previous snapshot (if exists)
            DatabaseSchema? previousSchema = null;
            if (SchemaSnapshot.Exists())
            {
                Console.WriteLine("Loading previous schema snapshot...");
                previousSchema = await SchemaSnapshot.LoadAsync();
                Console.WriteLine($"✓ Loaded snapshot with {previousSchema?.Tables.Count ?? 0} table(s)");
            }
            else
            {
                Console.WriteLine("ℹ️  No previous snapshot found - treating this as initial migration");
            }
            Console.WriteLine();

            // Generate migration
            Console.WriteLine("Generating migration code...");
            var scaffolder = new MigrationScaffolder();
            var migrationCode = scaffolder.GenerateMigration(previousSchema, currentSchema, name);

            // Save migration file
            var migrationsDir = FindOrCreateMigrationsDirectory();
            if (migrationsDir == null)
            {
                Console.WriteLine("Error: Could not find or create Migrations directory.");
                return;
            }

            var migrationId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var className = ToPascalCase(name);
            var fileName = $"{migrationId}_{className}.cs";
            var filePath = Path.Combine(migrationsDir, fileName);

            await File.WriteAllTextAsync(filePath, migrationCode);

            Console.WriteLine($"✓ Created migration: {fileName}");
            Console.WriteLine($"  Location: {filePath}");
            Console.WriteLine();

            // Save current schema as snapshot
            Console.WriteLine("Saving schema snapshot...");
            await SchemaSnapshot.SaveAsync(currentSchema);
            Console.WriteLine("✓ Schema snapshot saved");
            Console.WriteLine();

            Console.WriteLine("✨ Migration scaffolded successfully!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Review the generated migration file");
            Console.WriteLine("  2. Run migrations with: dotnet d1 database update");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }
    }
}
