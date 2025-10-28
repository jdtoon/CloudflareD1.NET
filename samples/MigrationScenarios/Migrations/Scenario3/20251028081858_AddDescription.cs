using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Adddescription
/// Created: 2025-10-28 08:18:58 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028081858_Adddescription : Migration
{
    public override string Id => "20251028081858";
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
        // Note: SQLite doesn't support DROP COLUMN. To remove 'description' from 'products', recreate the table.

    }
}
