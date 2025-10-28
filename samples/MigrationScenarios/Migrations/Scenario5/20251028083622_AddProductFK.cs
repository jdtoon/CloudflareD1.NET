using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Addproductfk
/// Created: 2025-10-28 08:36:22 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028083622_Addproductfk : Migration
{
    public override string Id => "20251028083622";
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
        // SQLite doesn't support DROP COLUMN directly. Using table recreation pattern.
        builder.RenameTable("order_items", "order_items_old");

        builder.CreateTable("order_items", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Integer("product_id").NotNull();
            t.Integer("quantity").NotNull();
        });

        builder.Sql("INSERT INTO order_items (id, product_id, quantity) SELECT id, product_id, quantity FROM order_items_old");

        builder.DropTable("order_items_old");

    }
}
