using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Adddescription
/// Created: 2025-10-28 08:36:22 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028083622_Adddescription : Migration
{
    public override string Id => "20251028083622";
    public override string Name => "Adddescription";

    public override void Up(MigrationBuilder builder)
    {
        builder.AlterTable("products", t =>
        {
            t.Text("description");
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        // SQLite doesn't support DROP COLUMN directly. Using table recreation pattern.
        builder.RenameTable("products", "products_old");

        builder.CreateTable("products", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("name").NotNull();
        });

        builder.Sql("INSERT INTO products (id, name) SELECT id, name FROM products_old");

        builder.DropTable("products_old");

    }
}
