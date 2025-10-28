using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Multiplechanges
/// Created: 2025-10-28 08:18:58 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028081858_Multiplechanges : Migration
{
    public override string Id => "20251028081858";
    public override string Name => "Multiplechanges";

    public override void Up(MigrationBuilder builder)
    {
        builder.AlterTable("products", t =>
        {
            t.Integer("stock").NotNull();
        });

        builder.CreateTable("reviews", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("text").NotNull();
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        // Note: SQLite doesn't support DROP COLUMN. To remove 'stock' from 'products', recreate the table.

        builder.DropTable("reviews");

    }
}
