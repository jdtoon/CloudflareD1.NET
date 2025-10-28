using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Addproductfk
/// Created: 2025-10-28 08:18:58 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028081858_Addproductfk : Migration
{
    public override string Id => "20251028081858";
    public override string Name => "Addproductfk";

    public override void Up(MigrationBuilder builder)
    {
        builder.AlterTable("order_items", t =>
        {
            t.Text("product");
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        // Note: SQLite doesn't support DROP COLUMN. To remove 'product' from 'order_items', recreate the table.

    }
}
